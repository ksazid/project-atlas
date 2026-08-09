using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Atlas.Api;

public sealed record ProgressiveQuestionAnswerRequest(
    string CatalogueVersion,
    IReadOnlyList<string>? Selections,
    string? Text)
{
    public const int AbsoluteMaxTextLength = 240;
}

public sealed record ProgressiveQuestionSkipRequest(string CatalogueVersion);

public sealed record ProgressiveQuestionSetResponse(
    string CatalogueKey,
    string CatalogueVersion,
    IReadOnlyList<ProgressiveQuestionDefinition> Questions);

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

public static class ProgressiveQuestionService
{
    public static async Task<ProgressiveQuestionSetResponse> GetAsync(
        AtlasDbContext db,
        string subject,
        Guid businessId,
        CancellationToken ct)
    {
        var business = await OwnedBusinessAsync(db, subject, businessId, ct);
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
        var business = await OwnedBusinessAsync(db, subject, businessId, ct);
        var definition = ResolveQuestion(business.Category, questionKey);
        var value = ValidateAndNormalizeAnswer(definition, request);

        var account = await OwnerAccountAsync(db, subject, businessId, ct)
            ?? throw NotFound();
        var existingProgress = await db.BusinessQuestionProgress.SingleOrDefaultAsync(x =>
            x.BusinessId == businessId &&
            x.CatalogueKey == ProgressiveQuestionCatalogueV1.CatalogueKey &&
            x.CatalogueVersion == ProgressiveQuestionCatalogueV1.Version &&
            x.QuestionKey == definition.QuestionKey, ct);

        if (existingProgress is not null)
        {
            if (existingProgress.Status == BusinessQuestionProgressStatuses.Answered)
            {
                var existingContext = await db.BusinessContextEntries.SingleOrDefaultAsync(x =>
                    x.BusinessId == businessId && x.Key == definition.TargetContextKey, ct);
                if (existingContext is not null && string.Equals(existingContext.Value, value, StringComparison.Ordinal))
                {
                    return new ProgressiveQuestionMutationResponse(
                        BusinessQuestionProgressStatuses.Answered,
                        definition.QuestionKey,
                        ProgressiveQuestionCatalogueV1.Version,
                        await BuildSetAsync(db, business, ct));
                }
            }

            throw new ProgressiveQuestionException(
                "progressive_question_completed",
                "This onboarding question has already been completed. Update Business Context instead.");
        }

        await EnsureQuestionIsEligibleAsync(db, business, definition, ct);

        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational()) transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var context = await db.BusinessContextEntries.SingleOrDefaultAsync(x =>
                x.BusinessId == businessId && x.Key == definition.TargetContextKey, ct);
            if (context is null)
            {
                context = new BusinessContextEntry
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    Key = definition.TargetContextKey,
                    Value = value,
                    Source = FieldSources.Owner,
                    OwnerConfirmed = true,
                    UpdatedAt = now
                };
                db.BusinessContextEntries.Add(context);
            }
            else
            {
                context.Value = value;
                context.Source = FieldSources.Owner;
                context.OwnerConfirmed = true;
                context.UpdatedAt = now;
            }

            db.BusinessQuestionProgress.Add(new BusinessQuestionProgress
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CatalogueKey = ProgressiveQuestionCatalogueV1.CatalogueKey,
                CatalogueVersion = ProgressiveQuestionCatalogueV1.Version,
                QuestionKey = definition.QuestionKey,
                Status = BusinessQuestionProgressStatuses.Answered,
                AnsweredContextKey = definition.TargetContextKey,
                CompletedAt = now
            });
            db.AuditRecords.Add(AuditRecord.Create(
                account.Id,
                businessId,
                $"business.progressive-question.answered:{definition.QuestionKey}"));

            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
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

        return new ProgressiveQuestionMutationResponse(
            BusinessQuestionProgressStatuses.Answered,
            definition.QuestionKey,
            ProgressiveQuestionCatalogueV1.Version,
            await BuildSetAsync(db, business, ct));
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
        var business = await OwnedBusinessAsync(db, subject, businessId, ct);
        var definition = ResolveQuestion(business.Category, questionKey);
        var account = await OwnerAccountAsync(db, subject, businessId, ct)
            ?? throw NotFound();

        var existingProgress = await db.BusinessQuestionProgress.SingleOrDefaultAsync(x =>
            x.BusinessId == businessId &&
            x.CatalogueKey == ProgressiveQuestionCatalogueV1.CatalogueKey &&
            x.CatalogueVersion == ProgressiveQuestionCatalogueV1.Version &&
            x.QuestionKey == definition.QuestionKey, ct);

        if (existingProgress is not null)
        {
            if (existingProgress.Status == BusinessQuestionProgressStatuses.Skipped)
            {
                return new ProgressiveQuestionMutationResponse(
                    BusinessQuestionProgressStatuses.Skipped,
                    definition.QuestionKey,
                    ProgressiveQuestionCatalogueV1.Version,
                    await BuildSetAsync(db, business, ct));
            }

            throw new ProgressiveQuestionException(
                "progressive_question_completed",
                "This onboarding question has already been completed. Update Business Context instead.");
        }

        await EnsureQuestionIsEligibleAsync(db, business, definition, ct);

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
            db.AuditRecords.Add(AuditRecord.Create(
                account.Id,
                businessId,
                $"business.progressive-question.skipped:{definition.QuestionKey}"));
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
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

        return new ProgressiveQuestionMutationResponse(
            BusinessQuestionProgressStatuses.Skipped,
            definition.QuestionKey,
            ProgressiveQuestionCatalogueV1.Version,
            await BuildSetAsync(db, business, ct));
    }

    private static async Task<ProgressiveQuestionSetResponse> BuildSetAsync(
        AtlasDbContext db,
        Business business,
        CancellationToken ct)
    {
        var context = await db.BusinessContextEntries
            .Where(x => x.BusinessId == business.Id)
            .ToListAsync(ct);
        var progress = await db.BusinessQuestionProgress
            .Where(x => x.BusinessId == business.Id)
            .ToListAsync(ct);
        return new ProgressiveQuestionSetResponse(
            ProgressiveQuestionCatalogueV1.CatalogueKey,
            ProgressiveQuestionCatalogueV1.Version,
            ProgressiveQuestionCatalogueV1.Select(business.Category, context, progress));
    }

    private static async Task EnsureQuestionIsEligibleAsync(
        AtlasDbContext db,
        Business business,
        ProgressiveQuestionDefinition definition,
        CancellationToken ct)
    {
        var set = await BuildSetAsync(db, business, ct);
        if (!set.Questions.Any(x => string.Equals(x.QuestionKey, definition.QuestionKey, StringComparison.OrdinalIgnoreCase)))
            throw new ProgressiveQuestionException(
                "progressive_question_not_found",
                "That onboarding question is not available for this Business.");
    }

    private static ProgressiveQuestionDefinition ResolveQuestion(string category, string questionKey)
    {
        if (string.IsNullOrWhiteSpace(questionKey)) throw NotFoundQuestion();
        var canonicalCategory = BusinessCategoryTaxonomy.IsKnownCategory(category)
            ? category.Trim().ToLowerInvariant()
            : BusinessCategoryTaxonomy.Generic.Key;
        var definition = ProgressiveQuestionCatalogueV1.Definitions.SingleOrDefault(x =>
            string.Equals(x.QuestionKey, questionKey.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (x.Categories.Contains(canonicalCategory) || x.Categories.Contains(BusinessCategoryTaxonomy.Generic.Key)));
        return definition ?? throw NotFoundQuestion();
    }

    private static string ValidateAndNormalizeAnswer(
        ProgressiveQuestionDefinition definition,
        ProgressiveQuestionAnswerRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (definition.AnswerType == ProgressiveQuestionAnswerTypes.ShortText)
        {
            var text = request.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                errors[nameof(ProgressiveQuestionAnswerRequest.Text)] = ["Enter a short answer or skip this question."];
            else if (text.Length > (definition.MaxLength ?? ProgressiveQuestionAnswerRequest.AbsoluteMaxTextLength))
                errors[nameof(ProgressiveQuestionAnswerRequest.Text)] = [$"Keep this answer to {definition.MaxLength ?? ProgressiveQuestionAnswerRequest.AbsoluteMaxTextLength} characters or fewer."];
            if (request.Selections is { Count: > 0 })
                errors[nameof(ProgressiveQuestionAnswerRequest.Selections)] = ["This question expects a text answer."];
            if (errors.Count > 0) throw new ProgressiveQuestionValidationException(errors);
            return text!;
        }

        var selections = request.Selections?
            .Select(x => x?.Trim() ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToList() ?? [];
        var duplicateCount = selections.Count - selections.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var maxSelections = definition.AnswerType == ProgressiveQuestionAnswerTypes.SingleChoice
            ? 1
            : definition.MaxSelections ?? definition.Options.Count;

        if (selections.Count == 0)
            errors[nameof(ProgressiveQuestionAnswerRequest.Selections)] = ["Choose an answer or skip this question."];
        else if (duplicateCount > 0)
            errors[nameof(ProgressiveQuestionAnswerRequest.Selections)] = ["Choose each answer only once."];
        else if (selections.Count > maxSelections)
            errors[nameof(ProgressiveQuestionAnswerRequest.Selections)] = [$"Choose no more than {maxSelections} answers."];
        else if (selections.Any(value => !definition.Options.Contains(value, StringComparer.OrdinalIgnoreCase)))
            errors[nameof(ProgressiveQuestionAnswerRequest.Selections)] = ["Choose only one of the available answers."];
        if (!string.IsNullOrWhiteSpace(request.Text))
            errors[nameof(ProgressiveQuestionAnswerRequest.Text)] = ["This question expects a choice answer."];
        if (errors.Count > 0) throw new ProgressiveQuestionValidationException(errors);

        var chosen = selections.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return string.Join(" | ", definition.Options.Where(chosen.Contains));
    }

    private static void EnsureCurrentVersion(string catalogueVersion)
    {
        if (!string.Equals(catalogueVersion?.Trim(), ProgressiveQuestionCatalogueV1.Version, StringComparison.Ordinal))
            throw new ProgressiveQuestionException(
                "progressive_catalogue_stale",
                "Refresh the onboarding questions before continuing.");
    }

    private static async Task<Business> OwnedBusinessAsync(
        AtlasDbContext db,
        string subject,
        Guid businessId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subject)) throw NotFound();
        var business = await db.Businesses.SingleOrDefaultAsync(x => x.Id == businessId, ct);
        if (business is null) throw NotFound();
        if (!await db.BusinessMemberships.AnyAsync(x =>
                x.BusinessId == businessId &&
                x.Role == MembershipRoles.BusinessOwner &&
                x.UserAccount.ProviderSubject == subject, ct))
            throw NotFound();
        return business;
    }

    private static async Task<UserAccount?> OwnerAccountAsync(
        AtlasDbContext db,
        string subject,
        Guid businessId,
        CancellationToken ct) =>
        (await db.BusinessMemberships
            .Include(x => x.UserAccount)
            .SingleOrDefaultAsync(x =>
                x.BusinessId == businessId &&
                x.Role == MembershipRoles.BusinessOwner &&
                x.UserAccount.ProviderSubject == subject, ct))?.UserAccount;

    private static ProgressiveQuestionException NotFound() => new(
        "progressive_questions_not_found",
        "Progressive onboarding is not available for this Business.");

    private static ProgressiveQuestionException NotFoundQuestion() => new(
        "progressive_question_not_found",
        "That onboarding question is not available for this Business.");
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
            catch (ProgressiveQuestionException ex) when (ex.Code == "progressive_questions_not_found")
            {
                return Results.NotFound(new { code = ex.Code, message = ex.Message });
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
            catch (ProgressiveQuestionException ex) when (ex.Code is "progressive_questions_not_found" or "progressive_question_not_found")
            {
                return Results.NotFound(new { code = ex.Code, message = ex.Message });
            }
            catch (ProgressiveQuestionException ex) when (ex.Code is "progressive_catalogue_stale" or "progressive_question_completed")
            {
                return Results.Conflict(new { code = ex.Code, message = ex.Message });
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
            catch (ProgressiveQuestionException ex) when (ex.Code is "progressive_questions_not_found" or "progressive_question_not_found")
            {
                return Results.NotFound(new { code = ex.Code, message = ex.Message });
            }
            catch (ProgressiveQuestionException ex) when (ex.Code is "progressive_catalogue_stale" or "progressive_question_completed")
            {
                return Results.Conflict(new { code = ex.Code, message = ex.Message });
            }
        }).RequireAuthorization("BusinessOwner");

        return app;
    }

    private static string? Subject(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
}
