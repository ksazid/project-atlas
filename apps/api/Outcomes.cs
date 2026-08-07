using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class OutcomeEvidenceClasses
{
    public const string Measured = "measured";
    public const string OwnerReported = "owner-reported";
    public const string Estimated = "estimated";
    public const string Unknown = "unknown";
    public static bool IsValid(string value) => value is Measured or OwnerReported or Estimated or Unknown;
}

public static class BusinessMemoryCategories
{
    public const string Outcome = "outcome";
}

public sealed class Outcome
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OpportunityId { get; set; }
    public Guid? GoalId { get; set; }
    public Guid KnowledgePackVersionId { get; set; }
    public required string KnowledgePackKey { get; set; }
    public required string KnowledgePackVersion { get; set; }
    public int UsefulnessRating { get; set; }
    public required string ResultSummary { get; set; }
    public int TimeSpentMinutes { get; set; }
    public string? OwnerNotes { get; set; }
    public string? MeasureName { get; set; }
    public decimal? MeasureValue { get; set; }
    public string? MeasureUnit { get; set; }
    public required string EvidenceClass { get; set; }
    public DateTimeOffset? FollowUpAt { get; set; }
    public Guid CapturedByUserAccountId { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint ConcurrencyVersion { get; set; }
}

public sealed class BusinessMemoryItem
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string StableKey { get; set; }
    public required string Category { get; set; }
    public required string SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public required string Value { get; set; }
    public bool IsDeletable { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint ConcurrencyVersion { get; set; }
}

public sealed record UpsertOutcomeRequest(
    int UsefulnessRating,
    string ResultSummary,
    int TimeSpentMinutes,
    string? OwnerNotes,
    string? MeasureName,
    decimal? MeasureValue,
    string? MeasureUnit,
    string EvidenceClass,
    DateTimeOffset? FollowUpAt,
    uint? Version)
{
    public Dictionary<string, string[]> Validate(DateTimeOffset now)
    {
        var errors = new Dictionary<string, string[]>();
        if (UsefulnessRating is < 1 or > 5) errors[nameof(UsefulnessRating)] = ["Usefulness rating must be between 1 and 5."];
        if (string.IsNullOrWhiteSpace(ResultSummary)) errors[nameof(ResultSummary)] = ["Result summary is required."];
        if (ResultSummary?.Length > 1000) errors[nameof(ResultSummary)] = ["Result summary must not exceed 1000 characters."];
        if (TimeSpentMinutes is < 0 or > 10080) errors[nameof(TimeSpentMinutes)] = ["Time spent must be between 0 and 10080 minutes."];
        if (OwnerNotes?.Length > 2000) errors[nameof(OwnerNotes)] = ["Owner notes must not exceed 2000 characters."];
        if (!OutcomeEvidenceClasses.IsValid(EvidenceClass)) errors[nameof(EvidenceClass)] = ["Evidence class must be measured, owner-reported, estimated or unknown."];

        var hasMeasure = !string.IsNullOrWhiteSpace(MeasureName) || MeasureValue.HasValue || !string.IsNullOrWhiteSpace(MeasureUnit);
        if (EvidenceClass == OutcomeEvidenceClasses.Measured && (!MeasureValue.HasValue || string.IsNullOrWhiteSpace(MeasureName)))
            errors[nameof(MeasureValue)] = ["Measured outcomes require a measure name and value."];
        if (hasMeasure && (string.IsNullOrWhiteSpace(MeasureName) || !MeasureValue.HasValue))
            errors[nameof(MeasureName)] = ["Measurable results require both a measure name and value."];
        if (FollowUpAt.HasValue && FollowUpAt.Value < now.AddMinutes(-5))
            errors[nameof(FollowUpAt)] = ["Follow-up date cannot be in the past."];
        return errors;
    }
}

public sealed record OutcomeResponse(
    Guid Id, Guid OpportunityId, int UsefulnessRating, string ResultSummary, int TimeSpentMinutes,
    string? OwnerNotes, string? MeasureName, decimal? MeasureValue, string? MeasureUnit, string EvidenceClass,
    DateTimeOffset? FollowUpAt, DateTimeOffset CapturedAt, DateTimeOffset UpdatedAt,
    string KnowledgePackKey, string KnowledgePackVersion, uint Version)
{
    public static OutcomeResponse From(Outcome value) => new(
        value.Id, value.OpportunityId, value.UsefulnessRating, value.ResultSummary, value.TimeSpentMinutes,
        value.OwnerNotes, value.MeasureName, value.MeasureValue, value.MeasureUnit, value.EvidenceClass,
        value.FollowUpAt, value.CapturedAt, value.UpdatedAt, value.KnowledgePackKey, value.KnowledgePackVersion, value.ConcurrencyVersion);
}

public sealed record BusinessMemoryResponse(Guid Id, string StableKey, string Category, string SourceType, Guid? SourceId, string Value, bool IsDeletable, DateTimeOffset UpdatedAt, uint Version);

public static class OutcomePolicy
{
    public static bool CanCapture(Opportunity opportunity, DateTimeOffset now) =>
        OpportunityPolicy.StatusFor(opportunity, now) == ActionStatuses.Completed;
}

public static class OutcomeEndpoints
{
    private static string? Subject(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

    private static async Task<UserAccount?> OwnerAccount(Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        var subject = Subject(user);
        if (string.IsNullOrWhiteSpace(subject)) return null;
        var membership = await db.BusinessMemberships.Include(x => x.UserAccount)
            .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.UserAccount.ProviderSubject == subject && x.Role == MembershipRoles.BusinessOwner, ct);
        return membership?.UserAccount;
    }

    public static IEndpointRouteBuilder MapOutcomeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/businesses/{businessId:guid}/opportunities/{opportunityId:guid}/outcome", async (
            Guid businessId, Guid opportunityId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            if (await OwnerAccount(businessId, user, db, ct) is null) return Results.NotFound();
            var outcome = await db.Outcomes.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.OpportunityId == opportunityId, ct);
            return outcome is null ? Results.NotFound() : Results.Ok(OutcomeResponse.From(outcome));
        }).RequireAuthorization("BusinessOwner");

        app.MapPut("/api/v1/businesses/{businessId:guid}/opportunities/{opportunityId:guid}/outcome", async (
            Guid businessId, Guid opportunityId, UpsertOutcomeRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();
            var errors = request.Validate(DateTimeOffset.UtcNow);
            if (errors.Count > 0) return Results.ValidationProblem(errors, extensions: new Dictionary<string, object?> { ["code"] = "outcome_invalid" });

            var opportunity = await db.Set<Opportunity>().SingleOrDefaultAsync(x => x.Id == opportunityId && x.BusinessId == businessId, ct);
            if (opportunity is null) return Results.NotFound();
            if (!OutcomePolicy.CanCapture(opportunity, DateTimeOffset.UtcNow))
                return Results.Conflict(new { code = "outcome_not_eligible", message = "Complete the Action before capturing an Outcome." });

            var outcome = await db.Outcomes.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.OpportunityId == opportunityId, ct);
            if (outcome is not null && request.Version != outcome.ConcurrencyVersion)
                return Results.Conflict(new { code = "outcome_stale", message = "The Outcome changed. Refresh before saving again." });
            if (outcome is null && request.Version.HasValue)
                return Results.Conflict(new { code = "outcome_stale", message = "The Outcome does not yet exist. Refresh before saving again." });

            var now = DateTimeOffset.UtcNow;
            if (outcome is null)
            {
                outcome = new Outcome
                {
                    Id = Guid.NewGuid(), BusinessId = businessId, OpportunityId = opportunity.Id, GoalId = opportunity.GoalId,
                    KnowledgePackVersionId = opportunity.KnowledgePackVersionId, KnowledgePackKey = opportunity.KnowledgePackKey,
                    KnowledgePackVersion = opportunity.KnowledgePackVersion, CapturedByUserAccountId = account.Id,
                    CapturedAt = now, ResultSummary = string.Empty, EvidenceClass = OutcomeEvidenceClasses.Unknown
                };
                db.Outcomes.Add(outcome);
            }

            outcome.UsefulnessRating = request.UsefulnessRating;
            outcome.ResultSummary = request.ResultSummary.Trim();
            outcome.TimeSpentMinutes = request.TimeSpentMinutes;
            outcome.OwnerNotes = request.OwnerNotes?.Trim();
            outcome.MeasureName = request.MeasureName?.Trim();
            outcome.MeasureValue = request.MeasureValue;
            outcome.MeasureUnit = request.MeasureUnit?.Trim();
            outcome.EvidenceClass = request.EvidenceClass;
            outcome.FollowUpAt = request.FollowUpAt;
            outcome.UpdatedAt = now;

            var memoryKey = $"outcome:{opportunity.Id}";
            var memory = await db.BusinessMemoryItems.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.StableKey == memoryKey, ct);
            var memoryValue = $"{outcome.EvidenceClass}: {outcome.ResultSummary}";
            if (memory is null)
            {
                memory = new BusinessMemoryItem
                {
                    Id = Guid.NewGuid(), BusinessId = businessId, StableKey = memoryKey, Category = BusinessMemoryCategories.Outcome,
                    SourceType = "outcome", SourceId = outcome.Id, Value = memoryValue, IsDeletable = true, CapturedAt = now, UpdatedAt = now
                };
                db.BusinessMemoryItems.Add(memory);
            }
            else
            {
                memory.SourceId = outcome.Id;
                memory.Value = memoryValue;
                memory.UpdatedAt = now;
            }

            db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"outcome.saved:{opportunity.Id}:{outcome.EvidenceClass}"));
            await db.SaveChangesAsync(ct);
            return Results.Ok(OutcomeResponse.From(outcome));
        }).RequireAuthorization("BusinessOwner");

        app.MapGet("/api/v1/businesses/{businessId:guid}/memory", async (
            Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            if (await OwnerAccount(businessId, user, db, ct) is null) return Results.NotFound();
            var items = await db.BusinessMemoryItems.Where(x => x.BusinessId == businessId).OrderByDescending(x => x.UpdatedAt)
                .Select(x => new BusinessMemoryResponse(x.Id, x.StableKey, x.Category, x.SourceType, x.SourceId, x.Value, x.IsDeletable, x.UpdatedAt, x.ConcurrencyVersion))
                .ToListAsync(ct);
            return Results.Ok(items);
        }).RequireAuthorization("BusinessOwner");

        app.MapDelete("/api/v1/businesses/{businessId:guid}/memory/{memoryId:guid}", async (
            Guid businessId, Guid memoryId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();
            var item = await db.BusinessMemoryItems.SingleOrDefaultAsync(x => x.Id == memoryId && x.BusinessId == businessId, ct);
            if (item is null) return Results.NotFound();
            if (!item.IsDeletable) return Results.Conflict(new { code = "memory_not_deletable", message = "This memory item is part of the required Business record." });
            db.BusinessMemoryItems.Remove(item);
            db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"memory.deleted:{item.StableKey}"));
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("BusinessOwner");

        return app;
    }
}
