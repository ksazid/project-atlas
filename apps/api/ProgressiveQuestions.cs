using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Atlas.Api;

public static class ProgressiveQuestionAnswerTypes
{
    public const string SingleChoice = "single-choice";
    public const string MultiChoice = "multi-choice";
    public const string ShortText = "short-text";
}

public static class BusinessQuestionProgressStatuses
{
    public const string Answered = "answered";
    public const string Skipped = "skipped";
}

public sealed record ProgressiveQuestionDefinition(
    string QuestionKey,
    string TargetContextKey,
    IReadOnlySet<string> Categories,
    int Priority,
    string Prompt,
    string? Helper,
    string AnswerType,
    IReadOnlyList<string> Options,
    int? MaxSelections,
    int? MaxLength,
    IReadOnlySet<string> MaterialityTags);

public sealed record ProgressiveQuestionResponse(
    string QuestionKey,
    string TargetContextKey,
    string Prompt,
    string? Helper,
    string AnswerType,
    IReadOnlyList<string> Options,
    int? MaxSelections,
    int? MaxLength)
{
    public static ProgressiveQuestionResponse From(ProgressiveQuestionDefinition definition) => new(
        definition.QuestionKey,
        definition.TargetContextKey,
        definition.Prompt,
        definition.Helper,
        definition.AnswerType,
        definition.Options,
        definition.MaxSelections,
        definition.MaxLength);
}

public sealed record ProgressiveQuestionSetResponse(
    string CatalogueKey,
    string CatalogueVersion,
    IReadOnlyList<ProgressiveQuestionResponse> Questions);

public sealed record ProgressiveQuestionAnswerRequest(
    string CatalogueVersion,
    IReadOnlyList<string>? Selections,
    string? Text);

public sealed record ProgressiveQuestionSkipRequest(string CatalogueVersion);

public sealed record ProgressiveQuestionMutationResponse(
    string Status,
    string QuestionKey,
    string CatalogueVersion,
    ProgressiveQuestionSetResponse Remaining);

public sealed class ProgressiveQuestionException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class ProgressiveQuestionValidationException(Dictionary<string, string[]> errors)
    : Exception("Progressive question answer is invalid.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}

public static class ProgressiveQuestionCatalogueV1
{
    public const string CatalogueKey = "progressive-onboarding";
    public const string Version = "1";

    private static readonly IReadOnlySet<string> GenericCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "generic-business" };

    public static IReadOnlyList<ProgressiveQuestionDefinition> Definitions { get; } =
    [
        CategoryChoice("restaurant-cafe.service-channel", "restaurant-cafe", "How do most customers order from you?", ["Dine in", "Takeaway", "Own website/app", "Marketplace/platform"], ["channel", "demand"]),
        CategoryChoice("beauty-personal-care.service-model", "beauty-personal-care", "How do customers usually receive your services?", ["Walk in", "Appointment", "At home/mobile", "Online"], ["channel", "capacity"]),
        CategoryChoice("retail.sales-channel", "retail", "Where do most customer purchases happen?", ["Physical shop", "Own website/app", "Marketplace/platform", "Phone/message"], ["channel", "demand"]),
        CategoryChoice("ecommerce.sales-channel", "ecommerce", "Where do most customer orders come from?", ["Own website/app", "Marketplace/platform", "Social media/message", "Wholesale/B2B"], ["channel", "demand"]),
        CategoryChoice("home-local-services.service-channel", "home-local-services", "How do customers usually book your services?", ["Phone", "Message/chat", "Website/form", "Marketplace/platform"], ["channel", "demand"]),
        CategoryChoice("professional-services.delivery-model", "professional-services", "How do clients usually work with you?", ["In person", "Remote/online", "On-site at client", "Mixed"], ["channel", "capacity"]),
        CategoryChoice("fitness-wellness.service-model", "fitness-wellness", "How do customers usually use your service?", ["Classes", "Appointments", "Open access/membership", "Online"], ["channel", "capacity"]),
        CategoryChoice("hospitality-accommodation.booking-channel", "hospitality-accommodation", "Where do most bookings come from?", ["Direct", "Own website", "Booking marketplace", "Agent/partner"], ["channel", "demand"]),

        new(
            "generic.primary-channel",
            "primarychannels",
            GenericCategory,
            100,
            "How do customers usually buy from you?",
            "This helps Atlas keep suggestions practical for the way you operate.",
            ProgressiveQuestionAnswerTypes.MultiChoice,
            ["In person", "Phone/message", "Own website/app", "Marketplace/platform"],
            3,
            null,
            Tags("channel", "demand")),
        new(
            "generic.busy-periods",
            "busyperiods",
            GenericCategory,
            90,
            "When are you usually busiest?",
            "A broad pattern is enough; Atlas does not need exact customer-level data.",
            ProgressiveQuestionAnswerTypes.MultiChoice,
            ["Weekday mornings", "Weekday afternoons", "Weekday evenings", "Weekends", "Seasonal/events"],
            2,
            null,
            Tags("demand", "capacity")),
        new(
            "generic.primary-constraint",
            "constraints",
            GenericCategory,
            80,
            "What limits the business most right now?",
            "Choose the constraint that most changes what is practical today.",
            ProgressiveQuestionAnswerTypes.SingleChoice,
            ["Time", "Staffing", "Capacity", "Cash/budget", "Demand", "Something else"],
            1,
            null,
            Tags("constraint", "capacity")),
        new(
            "generic.customer-groups",
            "customers",
            GenericCategory,
            70,
            "Who do you mainly serve?",
            "Describe customer groups at a business level, without names or personal details.",
            ProgressiveQuestionAnswerTypes.ShortText,
            [],
            null,
            240,
            Tags("customer")),
        new(
            "generic.current-priority",
            "currentpriorities",
            GenericCategory,
            60,
            "What deserves the most attention right now?",
            "A short near-term priority is enough.",
            ProgressiveQuestionAnswerTypes.ShortText,
            [],
            null,
            240,
            Tags("priority"))
    ];

    public static IReadOnlyList<ProgressiveQuestionDefinition> Select(
        string category,
        IReadOnlyCollection<BusinessContextEntry> context,
        IReadOnlyCollection<BusinessQuestionProgress> progress)
    {
        var canonicalCategory = BusinessCategoryTaxonomy.IsKnownCategory(category)
            ? category.Trim().ToLowerInvariant()
            : BusinessCategoryTaxonomy.Generic.Key;

        var authoritativeContextKeys = context
            .Where(x => x.OwnerConfirmed && !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => x.Key.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var completedQuestionKeys = progress
            .Where(x => string.Equals(x.CatalogueKey, CatalogueKey, StringComparison.OrdinalIgnoreCase) &&
                        (x.Status == BusinessQuestionProgressStatuses.Skipped || x.Status == BusinessQuestionProgressStatuses.Answered))
            .Select(x => x.QuestionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = Definitions
            .Where(question =>
                question.Categories.Contains(canonicalCategory) ||
                question.Categories.Contains(BusinessCategoryTaxonomy.Generic.Key))
            .Where(question => !authoritativeContextKeys.Contains(question.TargetContextKey))
            .Where(question => !completedQuestionKeys.Contains(question.QuestionKey))
            .OrderByDescending(question => question.Priority)
            .ThenByDescending(question => question.Categories.Contains(canonicalCategory) && !question.Categories.Contains(BusinessCategoryTaxonomy.Generic.Key))
            .ThenBy(question => question.QuestionKey, StringComparer.Ordinal)
            .ToList();

        var selected = new List<ProgressiveQuestionDefinition>(5);
        var targetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var question in candidates)
        {
            if (!targetKeys.Add(question.TargetContextKey)) continue;
            selected.Add(question);
            if (selected.Count == 5) break;
        }

        return selected;
    }

    private static ProgressiveQuestionDefinition CategoryChoice(
        string questionKey,
        string category,
        string prompt,
        IReadOnlyList<string> options,
        IReadOnlyList<string> tags) => new(
            questionKey,
            "primarychannels",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { category },
            120,
            prompt,
            "Choose the closest fit. You can change business context later.",
            ProgressiveQuestionAnswerTypes.MultiChoice,
            options,
            2,
            null,
            new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase));

    private static IReadOnlySet<string> Tags(params string[] values) => new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}

public static class ProgressiveQuestionService
{
    public static async Task<ProgressiveQuestionSetResponse> GetAsync(
        AtlasDbContext db,
        string subject,
        Guid businessId,
        CancellationToken ct)
    {
        var (_, business) = await OwnedBusinessAsync(db, subject, businessId, ct);
        return await BuildSetAsync(db, business, ct);
    }

    public static async Task<ProgressiveQuestionMutationResponse> AnswerAsync(
        AtlasDbContext db,
        string subject,
        Guid businessId,
        string questionKey,
        ProgressiveQuestionAnswerRequest request,
        CancellationToken ct)
    {
        EnsureCurrentVersion(request.CatalogueVersion);
        var (account, business) = await OwnedBusinessAsync(db, subject, businessId, ct);
        var definition = FindDefinition(questionKey);
        var normalizedValue = NormalizeAnswer(definition, request);

        var prior = await db.BusinessQuestionProgress
            .Where(x => x.BusinessId == businessId && x.CatalogueKey == ProgressiveQuestionCatalogueV1.CatalogueKey && x.QuestionKey == definition.QuestionKey)
            .OrderByDescending(x => x.CompletedAt)
            .FirstOrDefaultAsync(ct);

        if (prior is not null)
        {
            if (prior.Status == BusinessQuestionProgressStatuses.Answered)
            {
                var contextKey = prior.AnsweredContextKey ?? definition.TargetContextKey;
                var current = await db.BusinessContextEntries.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Key == contextKey, ct);
                if (current is not null && string.Equals(current.Value, normalizedValue, StringComparison.Ordinal))
                    return new ProgressiveQuestionMutationResponse(
                        BusinessQuestionProgressStatuses.Answered,
                        definition.QuestionKey,
                        ProgressiveQuestionCatalogueV1.Version,
                        await BuildSetAsync(db, business, ct));
            }

            throw new ProgressiveQuestionException("progressive_question_completed", "That optional question has already been completed.");
        }

        await EnsureEligibleAsync(db, business, definition.QuestionKey, ct);

        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational()) transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var context = await db.BusinessContextEntries.SingleOrDefaultAsync(
                x => x.BusinessId == businessId && x.Key == definition.TargetContextKey,
                ct);
            if (context is null)
            {
                context = new BusinessContextEntry
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    Key = definition.TargetContextKey,
                    Value = normalizedValue,
                    Source = FieldSources.Owner,
                    OwnerConfirmed = true,
                    UpdatedAt = now
                };
                db.BusinessContextEntries.Add(context);
            }
            else
            {
                context.Value = normalizedValue;
                context.Source = FieldSources.Owner;
                context.OwnerConfirmed = true;
                context.UpdatedAt = now;
            }

            db.BusinessQuestionProgress.Add(BusinessQuestionProgress.Answered(
                businessId,
                ProgressiveQuestionCatalogueV1.CatalogueKey,
                ProgressiveQuestionCatalogueV1.Version,
                definition.QuestionKey,
                definition.TargetContextKey,
                now));
            db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"business.progressive-question.answered:{definition.QuestionKey}"));

            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);

            return new ProgressiveQuestionMutationResponse(
                BusinessQuestionProgressStatuses.Answered,
                definition.QuestionKey,
                ProgressiveQuestionCatalogueV1.Version,
                await BuildSetAsync(db, business, ct));
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public static async Task<ProgressiveQuestionMutationResponse> SkipAsync(
        AtlasDbContext db,
        string subject,
        Guid businessId,
        string questionKey,
        string catalogueVersion,
        CancellationToken ct)
    {
        EnsureCurrentVersion(catalogueVersion);
        var (account, business) = await OwnedBusinessAsync(db, subject, businessId, ct);
        var definition = FindDefinition(questionKey);

        var prior = await db.BusinessQuestionProgress
            .Where(x => x.BusinessId == businessId && x.CatalogueKey == ProgressiveQuestionCatalogueV1.CatalogueKey && x.QuestionKey == definition.QuestionKey)
            .OrderByDescending(x => x.CompletedAt)
            .FirstOrDefaultAsync(ct);

        if (prior is not null)
        {
            if (prior.Status == BusinessQuestionProgressStatuses.Skipped)
                return new ProgressiveQuestionMutationResponse(
                    BusinessQuestionProgressStatuses.Skipped,
                    definition.QuestionKey,
                    ProgressiveQuestionCatalogueV1.Version,
                    await BuildSetAsync(db, business, ct));

            throw new ProgressiveQuestionException("progressive_question_completed", "That optional question has already been completed.");
        }

        await EnsureEligibleAsync(db, business, definition.QuestionKey, ct);

        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational()) transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow;
            db.BusinessQuestionProgress.Add(BusinessQuestionProgress.Skipped(
                businessId,
                ProgressiveQuestionCatalogueV1.CatalogueKey,
                ProgressiveQuestionCatalogueV1.Version,
                definition.QuestionKey,
                now));
            db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"business.progressive-question.skipped:{definition.QuestionKey}"));
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);

            return new ProgressiveQuestionMutationResponse(
                BusinessQuestionProgressStatuses.Skipped,
                definition.QuestionKey,
                ProgressiveQuestionCatalogueV1.Version,
                await BuildSetAsync(db, business, ct));
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private static async Task<(UserAccount Account, Business Business)> OwnedBusinessAsync(
        AtlasDbContext db,
        string subject,
        Guid businessId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ProgressiveQuestionException("progressive_questions_not_found", "Business questions are unavailable for this session.");

        var membership = await db.BusinessMemberships
            .Include(x => x.UserAccount)
            .SingleOrDefaultAsync(x =>
                x.BusinessId == businessId &&
                x.Role == MembershipRoles.BusinessOwner &&
                x.UserAccount.ProviderSubject == subject,
                ct);
        if (membership is null)
            throw new ProgressiveQuestionException("progressive_questions_not_found", "Business questions are unavailable for this Business.");

        var business = await db.Businesses.SingleOrDefaultAsync(x => x.Id == businessId, ct);
        if (business is null)
            throw new ProgressiveQuestionException("progressive_questions_not_found", "Business questions are unavailable for this Business.");

        return (membership.UserAccount, business);
    }

    private static async Task<ProgressiveQuestionSetResponse> BuildSetAsync(AtlasDbContext db, Business business, CancellationToken ct)
    {
        var context = await db.BusinessContextEntries.Where(x => x.BusinessId == business.Id).ToListAsync(ct);
        var progress = await db.BusinessQuestionProgress.Where(x => x.BusinessId == business.Id).ToListAsync(ct);
        var selected = ProgressiveQuestionCatalogueV1.Select(business.Category, context, progress)
            .Select(ProgressiveQuestionResponse.From)
            .ToList();
        return new ProgressiveQuestionSetResponse(
            ProgressiveQuestionCatalogueV1.CatalogueKey,
            ProgressiveQuestionCatalogueV1.Version,
            selected);
    }

    private static async Task EnsureEligibleAsync(AtlasDbContext db, Business business, string questionKey, CancellationToken ct)
    {
        var set = await BuildSetAsync(db, business, ct);
        if (!set.Questions.Any(x => string.Equals(x.QuestionKey, questionKey, StringComparison.Ordinal)))
            throw new ProgressiveQuestionException("progressive_question_not_found", "That optional question is no longer available. Refresh and continue.");
    }

    private static ProgressiveQuestionDefinition FindDefinition(string questionKey) =>
        ProgressiveQuestionCatalogueV1.Definitions.SingleOrDefault(x => string.Equals(x.QuestionKey, questionKey, StringComparison.Ordinal))
        ?? throw new ProgressiveQuestionException("progressive_question_not_found", "That optional question is unavailable. Refresh and continue.");

    private static void EnsureCurrentVersion(string catalogueVersion)
    {
        if (!string.Equals(catalogueVersion, ProgressiveQuestionCatalogueV1.Version, StringComparison.Ordinal))
            throw new ProgressiveQuestionException("progressive_catalogue_stale", "These optional questions changed. Refresh to continue with the latest set.");
    }

    private static string NormalizeAnswer(ProgressiveQuestionDefinition definition, ProgressiveQuestionAnswerRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var selections = request.Selections?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? [];

        if (definition.AnswerType == ProgressiveQuestionAnswerTypes.ShortText)
        {
            var text = request.Text?.Trim();
            if (selections.Count > 0) errors[nameof(request.Selections)] = ["Use the text field for this question."];
            if (string.IsNullOrWhiteSpace(text)) errors[nameof(request.Text)] = ["Enter a short answer or skip for now."];
            else if (text.Length > (definition.MaxLength ?? 240)) errors[nameof(request.Text)] = [$"Keep this answer to {definition.MaxLength ?? 240} characters or fewer."];
            if (errors.Count > 0) throw new ProgressiveQuestionValidationException(errors);
            return text!;
        }

        if (!string.IsNullOrWhiteSpace(request.Text)) errors[nameof(request.Text)] = ["Choose from the available options for this question."];
        if (selections.Count != selections.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            errors[nameof(request.Selections)] = ["Choose each option only once."];

        var maximum = definition.AnswerType == ProgressiveQuestionAnswerTypes.SingleChoice ? 1 : definition.MaxSelections ?? definition.Options.Count;
        if (selections.Count == 0 || selections.Count > maximum)
            errors[nameof(request.Selections)] = [$"Choose between 1 and {maximum} option{(maximum == 1 ? string.Empty : "s")}."];

        var canonical = definition.Options
            .Where(option => selections.Contains(option, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (canonical.Count != selections.Count)
            errors[nameof(request.Selections)] = ["Choose only from the available options."];

        if (errors.Count > 0) throw new ProgressiveQuestionValidationException(errors);
        return string.Join(", ", canonical);
    }
}

public static class ProgressiveQuestionEndpoints
{
    public static WebApplication MapProgressiveQuestionEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/businesses/{businessId:guid}/progressive-questions", async (
            Guid businessId,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var subject = Subject(user);
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            try
            {
                return Results.Ok(await ProgressiveQuestionService.GetAsync(db, subject, businessId, ct));
            }
            catch (ProgressiveQuestionException ex)
            {
                return Problem(ex);
            }
        }).RequireAuthorization("BusinessOwner");

        app.MapPost("/api/v1/businesses/{businessId:guid}/progressive-questions/{questionKey}/answer", async (
            Guid businessId,
            string questionKey,
            ProgressiveQuestionAnswerRequest request,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var subject = Subject(user);
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            try
            {
                return Results.Ok(await ProgressiveQuestionService.AnswerAsync(db, subject, businessId, questionKey, request, ct));
            }
            catch (ProgressiveQuestionValidationException ex)
            {
                return Results.ValidationProblem(ex.Errors, extensions: new Dictionary<string, object?> { ["code"] = "progressive_question_invalid" });
            }
            catch (ProgressiveQuestionException ex)
            {
                return Problem(ex);
            }
        }).RequireAuthorization("BusinessOwner");

        app.MapPost("/api/v1/businesses/{businessId:guid}/progressive-questions/{questionKey}/skip", async (
            Guid businessId,
            string questionKey,
            ProgressiveQuestionSkipRequest request,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var subject = Subject(user);
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            try
            {
                return Results.Ok(await ProgressiveQuestionService.SkipAsync(db, subject, businessId, questionKey, request.CatalogueVersion, ct));
            }
            catch (ProgressiveQuestionException ex)
            {
                return Problem(ex);
            }
        }).RequireAuthorization("BusinessOwner");

        return app;
    }

    private static IResult Problem(ProgressiveQuestionException ex) => ex.Code switch
    {
        "progressive_questions_not_found" or "progressive_question_not_found" => Results.NotFound(new { code = ex.Code, message = ex.Message }),
        "progressive_catalogue_stale" or "progressive_question_completed" => Results.Conflict(new { code = ex.Code, message = ex.Message }),
        _ => Results.BadRequest(new { code = ex.Code, message = ex.Message })
    };

    private static string? Subject(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
}
