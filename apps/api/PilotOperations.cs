using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed class IntelligenceRunRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? ActorUserAccountId { get; set; }
    public required string Outcome { get; set; }
    public string? Code { get; set; }
    public int CandidateCount { get; set; }
    public Guid? OpportunityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class PilotOperationRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OperatorUserAccountId { get; set; }
    public required string Action { get; set; }
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? Reason { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public static class PilotOperationActions
{
    public const string SupportNote = "support-note";
    public const string ProfileCorrection = "profile-correction";
    public const string OpportunityPrepared = "opportunity-prepared";
    public const string OpportunityWithdrawn = "opportunity-withdrawn";
}

public sealed record PilotSupportNoteRequest(string Note);
public sealed record PilotWithdrawRequest(string Reason, uint Version);
public sealed record PilotProfileCorrectionRequest(
    string? Description,
    string? Address,
    string? Website,
    string? Phone,
    string? Email,
    string? SocialChannels,
    string? BusinessHours,
    string Language,
    string Reason);

public sealed record PilotBusinessListItem(
    Guid BusinessId,
    string Name,
    string Category,
    string PrimaryLocation,
    bool ProfileConfirmed,
    int GoalCount,
    Guid? CurrentOpportunityId,
    string? CurrentOpportunityTitle,
    string? CurrentOpportunityStatus,
    string? LatestGenerationOutcome,
    string? LatestGenerationCode,
    DateTimeOffset? LatestGenerationAt,
    int UnsafeFeedbackCount,
    int UsefulFeedbackCount,
    int NotUsefulFeedbackCount,
    DateTimeOffset? LatestOperatorActivityAt);

public sealed record PilotBusinessDetail(
    BusinessResponse Business,
    BusinessProfile? Profile,
    int GoalCount,
    int ContextEntryCount,
    IReadOnlyList<Opportunity> Opportunities,
    IReadOnlyList<IntelligenceRunRecord> GenerationHistory,
    IReadOnlyList<FeedbackRecord> Feedback,
    IReadOnlyList<PilotOperationRecord> Operations);

public static class PilotOperationsPolicy
{
    public static Dictionary<string, string[]> ValidateSupportNote(PilotSupportNoteRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var note = NormalizeText(request.Note);
        if (note is null)
            errors[nameof(request.Note)] = ["Support note is required."];
        else if (note.Length > 2000)
            errors[nameof(request.Note)] = ["Support note must be 2000 characters or fewer."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateWithdrawal(PilotWithdrawRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var reason = NormalizeText(request.Reason);
        if (reason is null)
            errors[nameof(request.Reason)] = ["Withdrawal reason is required."];
        else if (reason.Length > 2000)
            errors[nameof(request.Reason)] = ["Withdrawal reason must be 2000 characters or fewer."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateProfileCorrection(PilotProfileCorrectionRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Language))
            errors[nameof(request.Language)] = ["Language is required."];
        var reason = NormalizeText(request.Reason);
        if (reason is null)
            errors[nameof(request.Reason)] = ["Correction reason is required."];
        else if (reason.Length > 2000)
            errors[nameof(request.Reason)] = ["Correction reason must be 2000 characters or fewer."];
        return errors;
    }

    public static string? NormalizeText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

public static class PilotOperationsService
{
    public static async Task<UserAccount?> ResolveOperatorAsync(
        AtlasDbContext db,
        ClaimsPrincipal user,
        DateTimeOffset now,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(user);
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject)) return null;

        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.ProviderSubject == subject, ct);
        if (account is not null) return account;

        account = new UserAccount
        {
            Id = Guid.NewGuid(),
            ProviderSubject = subject,
            CreatedAt = now
        };
        db.UserAccounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account;
    }

    public static async Task<IReadOnlyList<PilotBusinessListItem>> ListBusinessesAsync(
        AtlasDbContext db,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        var businesses = await db.Businesses.AsNoTracking().ToListAsync(ct);
        var profiles = await db.BusinessProfiles.AsNoTracking().ToListAsync(ct);
        var goals = await db.BusinessGoals.AsNoTracking().ToListAsync(ct);
        var opportunities = await db.Opportunities.AsNoTracking().ToListAsync(ct);
        var diagnostics = await db.IntelligenceRuns.AsNoTracking().ToListAsync(ct);
        var feedback = await db.FeedbackRecords.AsNoTracking().ToListAsync(ct);
        var operations = await db.PilotOperationRecords.AsNoTracking().ToListAsync(ct);

        var items = businesses.Select(business =>
        {
            var profile = profiles.SingleOrDefault(x => x.BusinessId == business.Id);
            var currentOpportunity = opportunities
                .Where(x => x.BusinessId == business.Id)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();
            var latestDiagnostic = diagnostics
                .Where(x => x.BusinessId == business.Id)
                .OrderByDescending(x => x.OccurredAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();
            var businessFeedback = feedback.Where(x => x.BusinessId == business.Id).ToList();
            var latestOperatorActivity = operations
                .Where(x => x.BusinessId == business.Id)
                .Select(x => (DateTimeOffset?)x.OccurredAt)
                .Max();

            return new PilotBusinessListItem(
                business.Id,
                business.Name,
                business.Category,
                business.PrimaryLocation,
                profile?.OwnerConfirmed == true,
                goals.Count(x => x.BusinessId == business.Id),
                currentOpportunity?.Id,
                currentOpportunity?.Title,
                currentOpportunity?.Status,
                latestDiagnostic?.Outcome,
                latestDiagnostic?.Code,
                latestDiagnostic?.OccurredAt,
                businessFeedback.Count(x => x.Kind == FeedbackKinds.UnsafeGuidance),
                businessFeedback.Count(x => x.Kind == FeedbackKinds.OpportunityRating && x.Usefulness == FeedbackUsefulnessValues.Useful),
                businessFeedback.Count(x => x.Kind == FeedbackKinds.OpportunityRating && x.Usefulness == FeedbackUsefulnessValues.NotUseful),
                latestOperatorActivity);
        }).ToList();

        return items
            .OrderByDescending(x => x.UnsafeFeedbackCount > 0)
            .ThenByDescending(x => x.LatestGenerationOutcome == OpportunityFocusGenerationStates.Degraded)
            .ThenByDescending(x => x.LatestGenerationOutcome is OpportunityFocusGenerationStates.NoFocus or OpportunityFocusGenerationStates.InsufficientContext)
            .ThenByDescending(x => x.LatestGenerationAt ?? x.LatestOperatorActivityAt)
            .ThenBy(x => x.BusinessId)
            .Take(50)
            .ToList();
    }

    public static async Task<PilotBusinessDetail?> GetBusinessAsync(
        AtlasDbContext db,
        Guid businessId,
        CancellationToken ct)
    {
        var business = await db.Businesses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == businessId, ct);
        if (business is null) return null;

        var profile = await db.BusinessProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);
        var goalCount = await db.BusinessGoals.AsNoTracking().CountAsync(x => x.BusinessId == businessId, ct);
        var contextCount = await db.BusinessContextEntries.AsNoTracking().CountAsync(x => x.BusinessId == businessId, ct);
        var opportunities = await db.Opportunities.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(20)
            .ToListAsync(ct);
        var generationHistory = await db.IntelligenceRuns.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Take(20)
            .ToListAsync(ct);
        var feedback = await db.FeedbackRecords.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(20)
            .ToListAsync(ct);
        var operations = await db.PilotOperationRecords.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Take(20)
            .ToListAsync(ct);

        return new PilotBusinessDetail(
            BusinessResponse.From(business),
            profile,
            goalCount,
            contextCount,
            opportunities,
            generationHistory,
            feedback,
            operations);
    }

    public static async Task<PilotOperationRecord?> AddSupportNoteAsync(
        AtlasDbContext db,
        Guid businessId,
        UserAccount operatorAccount,
        PilotSupportNoteRequest request,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (PilotOperationsPolicy.ValidateSupportNote(request).Count > 0) return null;
        if (!await db.Businesses.AnyAsync(x => x.Id == businessId, ct)) return null;

        var record = new PilotOperationRecord
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            OperatorUserAccountId = operatorAccount.Id,
            Action = PilotOperationActions.SupportNote,
            Reason = PilotOperationsPolicy.NormalizeText(request.Note),
            OccurredAt = now
        };
        db.PilotOperationRecords.Add(record);
        db.AuditRecords.Add(AuditRecord.Create(operatorAccount.Id, businessId, "pilot-operations.support-note.created"));
        await db.SaveChangesAsync(ct);
        return record;
    }

    public static async Task<BusinessProfile?> CorrectProfileAsync(
        AtlasDbContext db,
        Guid businessId,
        UserAccount operatorAccount,
        PilotProfileCorrectionRequest request,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (PilotOperationsPolicy.ValidateProfileCorrection(request).Count > 0) return null;
        if (!await db.Businesses.AnyAsync(x => x.Id == businessId, ct)) return null;

        var profile = await db.BusinessProfiles.SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);
        if (profile is null)
        {
            profile = new BusinessProfile
            {
                BusinessId = businessId,
                Language = request.Language.Trim(),
                Source = FieldSources.OperatorAssisted,
                OwnerConfirmed = false,
                UpdatedAt = now
            };
            db.BusinessProfiles.Add(profile);
        }

        var changedFields = new List<string>();
        Apply(nameof(profile.Description), profile.Description, request.Description, value => profile.Description = value, changedFields);
        Apply(nameof(profile.Address), profile.Address, request.Address, value => profile.Address = value, changedFields);
        Apply(nameof(profile.Website), profile.Website, request.Website, value => profile.Website = value, changedFields);
        Apply(nameof(profile.Phone), profile.Phone, request.Phone, value => profile.Phone = value, changedFields);
        Apply(nameof(profile.Email), profile.Email, request.Email, value => profile.Email = value, changedFields);
        Apply(nameof(profile.SocialChannels), profile.SocialChannels, request.SocialChannels, value => profile.SocialChannels = value, changedFields);
        Apply(nameof(profile.BusinessHours), profile.BusinessHours, request.BusinessHours, value => profile.BusinessHours = value, changedFields);
        var language = request.Language.Trim();
        if (!string.Equals(profile.Language, language, StringComparison.Ordinal))
        {
            profile.Language = language;
            changedFields.Add(nameof(profile.Language));
        }

        profile.Source = FieldSources.OperatorAssisted;
        profile.OwnerConfirmed = false;
        profile.UpdatedAt = now;

        var operation = new PilotOperationRecord
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            OperatorUserAccountId = operatorAccount.Id,
            Action = PilotOperationActions.ProfileCorrection,
            TargetType = nameof(BusinessProfile),
            TargetId = businessId,
            Reason = PilotOperationsPolicy.NormalizeText(request.Reason),
            MetadataJson = JsonSerializer.Serialize(new { changedFields }),
            OccurredAt = now
        };
        db.PilotOperationRecords.Add(operation);
        db.AuditRecords.Add(AuditRecord.Create(operatorAccount.Id, businessId, "pilot-operations.profile.corrected"));
        await db.SaveChangesAsync(ct);
        return profile;
    }

    private static void Apply(
        string field,
        string? current,
        string? proposed,
        Action<string?> assign,
        ICollection<string> changedFields)
    {
        var normalized = PilotOperationsPolicy.NormalizeText(proposed);
        if (string.Equals(current, normalized, StringComparison.Ordinal)) return;
        assign(normalized);
        changedFields.Add(field);
    }
}

public static class PilotOperationsEndpoints
{
    public static void MapPilotOperationsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/pilot-operations/businesses", async (
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var account = await PilotOperationsService.ResolveOperatorAsync(db, user, DateTimeOffset.UtcNow, ct);
            if (account is null) return Results.Unauthorized();
            return Results.Ok(await PilotOperationsService.ListBusinessesAsync(db, ct));
        }).RequireAuthorization("InternalOperator");

        app.MapGet("/api/v1/pilot-operations/businesses/{businessId:guid}", async (
            Guid businessId,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var account = await PilotOperationsService.ResolveOperatorAsync(db, user, DateTimeOffset.UtcNow, ct);
            if (account is null) return Results.Unauthorized();
            var detail = await PilotOperationsService.GetBusinessAsync(db, businessId, ct);
            if (detail is null) return Results.NotFound();
            db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, "pilot-operations.business.viewed"));
            await db.SaveChangesAsync(ct);
            return Results.Ok(detail);
        }).RequireAuthorization("InternalOperator");

        app.MapPost("/api/v1/pilot-operations/businesses/{businessId:guid}/notes", async (
            Guid businessId,
            PilotSupportNoteRequest request,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var errors = PilotOperationsPolicy.ValidateSupportNote(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors, extensions: new Dictionary<string, object?> { ["code"] = "pilot_note_invalid" });
            var account = await PilotOperationsService.ResolveOperatorAsync(db, user, DateTimeOffset.UtcNow, ct);
            if (account is null) return Results.Unauthorized();
            var record = await PilotOperationsService.AddSupportNoteAsync(db, businessId, account, request, DateTimeOffset.UtcNow, ct);
            return record is null ? Results.NotFound() : Results.Created($"/api/v1/pilot-operations/businesses/{businessId}/notes/{record.Id}", record);
        }).RequireAuthorization("InternalOperator");

        app.MapPut("/api/v1/pilot-operations/businesses/{businessId:guid}/profile", async (
            Guid businessId,
            PilotProfileCorrectionRequest request,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var errors = PilotOperationsPolicy.ValidateProfileCorrection(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors, extensions: new Dictionary<string, object?> { ["code"] = "pilot_profile_invalid" });
            var account = await PilotOperationsService.ResolveOperatorAsync(db, user, DateTimeOffset.UtcNow, ct);
            if (account is null) return Results.Unauthorized();
            var profile = await PilotOperationsService.CorrectProfileAsync(db, businessId, account, request, DateTimeOffset.UtcNow, ct);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        }).RequireAuthorization("InternalOperator");
    }
}
