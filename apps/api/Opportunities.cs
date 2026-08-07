using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class OpportunityStatuses
{
    public const string Available = "available";
    public const string Applied = "applied";
    public const string Skipped = "skipped";
    public const string NotRelevant = "not-relevant";
    public const string Expired = "expired";
}

public static class OpportunityDecisions
{
    public const string Apply = "apply";
    public const string Skip = "skip";
    public const string NotRelevant = "not-relevant";
    public static bool IsValid(string value) => value is Apply or Skip or NotRelevant;
}

public sealed class Opportunity
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string Title { get; set; }
    public required string WhyItMatters { get; set; }
    public required string WhyNow { get; set; }
    public required string ExpectedImpact { get; set; }
    public required string Effort { get; set; }
    public required string Confidence { get; set; }
    public required string EvidenceSummary { get; set; }
    public required string EvidenceJson { get; set; }
    public required string Status { get; set; }
    public required string KnowledgePackKey { get; set; }
    public required string KnowledgePackVersion { get; set; }
    public Guid KnowledgePackVersionId { get; set; }
    public Guid? GoalId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public Guid? DecidedByUserAccountId { get; set; }
    public string? DecisionReason { get; set; }
    public uint ConcurrencyVersion { get; set; }
}

public sealed record OpportunityDecisionRequest(string Decision, string? Reason, uint Version)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (!OpportunityDecisions.IsValid(Decision)) errors[nameof(Decision)] = ["Decision must be apply, skip or not-relevant."];
        if (Decision is OpportunityDecisions.Skip or OpportunityDecisions.NotRelevant && string.IsNullOrWhiteSpace(Reason))
            errors[nameof(Reason)] = ["A reason is required for skip and not-relevant decisions."];
        return errors;
    }
}

public sealed record TodayFocusResponse(
    Guid Id,
    string Title,
    string WhyItMatters,
    string WhyNow,
    string ExpectedImpact,
    string Effort,
    string Confidence,
    string EvidenceSummary,
    string Status,
    DateTimeOffset ExpiresAt,
    string KnowledgePackKey,
    string KnowledgePackVersion,
    uint Version)
{
    public static TodayFocusResponse From(Opportunity value) => new(
        value.Id, value.Title, value.WhyItMatters, value.WhyNow, value.ExpectedImpact,
        value.Effort, value.Confidence, value.EvidenceSummary, value.Status,
        value.ExpiresAt, value.KnowledgePackKey, value.KnowledgePackVersion, value.ConcurrencyVersion);
}

public static class OpportunityPolicy
{
    public static bool IsEligible(BusinessProfile? profile, IReadOnlyCollection<BusinessGoal> goals, BusinessKnowledgeAssignment? assignment) =>
        profile is { OwnerConfirmed: true } && goals.Count > 0 && assignment is { IsCurrent: true };

    public static string StatusFor(Opportunity value, DateTimeOffset now) =>
        value.Status == OpportunityStatuses.Available && value.ExpiresAt <= now ? OpportunityStatuses.Expired : value.Status;

    public static bool CanDecide(Opportunity value, DateTimeOffset now) =>
        value.Status == OpportunityStatuses.Available && value.ExpiresAt > now;
}

public static class OpportunityEndpoints
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

    private static async Task<Opportunity?> CreateDeterministicFocus(Guid businessId, UserAccount account, AtlasDbContext db, CancellationToken ct)
    {
        var profile = await db.BusinessProfiles.SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);
        var goals = await db.BusinessGoals.Where(x => x.BusinessId == businessId).OrderBy(x => x.Priority).ToListAsync(ct);
        var assignment = await db.BusinessKnowledgeAssignments.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.IsCurrent, ct);
        if (!OpportunityPolicy.IsEligible(profile, goals, assignment)) return null;

        var primaryGoal = goals[0];
        var business = await db.Businesses.SingleAsync(x => x.Id == businessId, ct);
        var now = DateTimeOffset.UtcNow;
        var focus = new Opportunity
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Title = $"Review one practical action for {primaryGoal.Title}",
            WhyItMatters = $"This supports your highest-priority goal: {primaryGoal.Title}.",
            WhyNow = "Your profile, goal and active Knowledge Pack provide enough confirmed context for a focused review.",
            ExpectedImpact = "Clarify one measurable next action without committing to an unsupported result.",
            Effort = "Low",
            Confidence = "Medium",
            EvidenceSummary = $"Confirmed business profile; priority goal #{primaryGoal.Priority}; active {assignment!.PackKey} Knowledge Pack v{assignment.ExactVersion}.",
            EvidenceJson = System.Text.Json.JsonSerializer.Serialize(new { profile = "owner-confirmed", goalId = primaryGoal.Id, goal = primaryGoal.Title, assignment.PackKey, assignment.ExactVersion }),
            Status = OpportunityStatuses.Available,
            KnowledgePackKey = assignment.PackKey,
            KnowledgePackVersion = assignment.ExactVersion,
            KnowledgePackVersionId = assignment.KnowledgePackVersionId,
            GoalId = primaryGoal.Id,
            CreatedAt = now,
            ExpiresAt = now.AddDays(1)
        };
        db.Set<Opportunity>().Add(focus);
        db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"opportunity.created:{focus.Id}"));
        await db.SaveChangesAsync(ct);
        return focus;
    }

    public static IEndpointRouteBuilder MapOpportunityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/businesses/{businessId:guid}/today-focus", async (Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();

            var now = DateTimeOffset.UtcNow;
            var current = await db.Set<Opportunity>()
                .Where(x => x.BusinessId == businessId && x.Status == OpportunityStatuses.Available)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (current is not null && current.ExpiresAt <= now)
            {
                current.Status = OpportunityStatuses.Expired;
                await db.SaveChangesAsync(ct);
                current = null;
            }

            current ??= await CreateDeterministicFocus(businessId, account, db, ct);
            if (current is null)
                return Results.Ok(new { state = "insufficient-context", message = "Confirm your Business Profile, choose at least one goal and keep an active Knowledge Pack to receive Today’s Focus." });

            return Results.Ok(new { state = "ready", opportunity = TodayFocusResponse.From(current) });
        }).RequireAuthorization("BusinessOwner");

        app.MapPost("/api/v1/businesses/{businessId:guid}/opportunities/{opportunityId:guid}/decision", async (Guid businessId, Guid opportunityId, OpportunityDecisionRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();
            var errors = request.Validate();
            if (errors.Count > 0) return Results.ValidationProblem(errors, extensions: new Dictionary<string, object?> { ["code"] = "opportunity_decision_invalid" });

            var opportunity = await db.Set<Opportunity>().SingleOrDefaultAsync(x => x.Id == opportunityId && x.BusinessId == businessId, ct);
            if (opportunity is null) return Results.NotFound();
            if (opportunity.ConcurrencyVersion != request.Version)
                return Results.Conflict(new { code = "opportunity_stale", message = "Today’s Focus changed. Refresh before deciding." });
            if (!OpportunityPolicy.CanDecide(opportunity, DateTimeOffset.UtcNow))
                return Results.Conflict(new { code = "opportunity_not_actionable", message = "This Opportunity is no longer actionable." });

            opportunity.Status = request.Decision switch
            {
                OpportunityDecisions.Apply => OpportunityStatuses.Applied,
                OpportunityDecisions.Skip => OpportunityStatuses.Skipped,
                _ => OpportunityStatuses.NotRelevant
            };
            opportunity.DecisionReason = request.Reason?.Trim();
            opportunity.DecidedAt = DateTimeOffset.UtcNow;
            opportunity.DecidedByUserAccountId = account.Id;
            db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"opportunity.decision:{opportunity.Id}:{opportunity.Status}"));
            await db.SaveChangesAsync(ct);
            return Results.Ok(TodayFocusResponse.From(opportunity));
        }).RequireAuthorization("BusinessOwner");

        return app;
    }
}
