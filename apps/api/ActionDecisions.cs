using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class ActionStatuses
{
    public const string Applied = "applied";
    public const string Completed = "completed";
    public const string Skipped = "skipped";
    public const string NotRelevant = "not-relevant";
    public const string Rejected = "rejected";

    public static bool IsValid(string value) => value is Applied or Completed or Skipped or NotRelevant or Rejected;
    public static bool IsTerminal(string value) => value is Completed or Skipped or NotRelevant or Rejected;
}

public static class ActionDecisionReasonCodes
{
    public const string TimingNotRight = "timing-not-right";
    public const string AlreadyDone = "already-done";
    public const string InsufficientCapacity = "insufficient-capacity";
    public const string NotAPriority = "not-a-priority";
    public const string ContextIncorrect = "context-incorrect";
    public const string RecommendationNotRelevant = "recommendation-not-relevant";
    public const string UnsafeOrInappropriate = "unsafe-or-inappropriate";
    public const string Other = "other";

    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        TimingNotRight, AlreadyDone, InsufficientCapacity, NotAPriority,
        ContextIncorrect, RecommendationNotRelevant, UnsafeOrInappropriate, Other
    };

    public static bool IsValid(string? value) => value is not null && Allowed.Contains(value);
}

public sealed class ActionDecisionRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OpportunityId { get; set; }
    public Guid? GoalId { get; set; }
    public Guid KnowledgePackVersionId { get; set; }
    public required string KnowledgePackKey { get; set; }
    public required string KnowledgePackVersion { get; set; }
    public required string Status { get; set; }
    public string? ReasonCode { get; set; }
    public string? OwnerNote { get; set; }
    public Guid DecidedByUserAccountId { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
    public uint OpportunityVersionBeforeDecision { get; set; }
}

public sealed record RecordActionDecisionRequest(string Status, string? ReasonCode, string? OwnerNote, uint Version)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (!ActionStatuses.IsValid(Status))
            errors[nameof(Status)] = ["Status must be applied, completed, skipped, not-relevant or rejected."];

        var requiresReason = Status is ActionStatuses.Skipped or ActionStatuses.NotRelevant or ActionStatuses.Rejected;
        if (requiresReason && !ActionDecisionReasonCodes.IsValid(ReasonCode))
            errors[nameof(ReasonCode)] = ["A supported reason code is required for skipped, not-relevant and rejected decisions."];
        if (!requiresReason && !string.IsNullOrWhiteSpace(ReasonCode))
            errors[nameof(ReasonCode)] = ["Reason codes are supported only for skipped, not-relevant and rejected decisions."];
        if (ReasonCode == ActionDecisionReasonCodes.Other && string.IsNullOrWhiteSpace(OwnerNote))
            errors[nameof(OwnerNote)] = ["An owner note is required when reason code is other."];
        if (OwnerNote?.Length > 1000)
            errors[nameof(OwnerNote)] = ["Owner note must not exceed 1000 characters."];
        return errors;
    }
}

public sealed record ActionDecisionItem(Guid Id, string Status, string? ReasonCode, string? OwnerNote, DateTimeOffset DecidedAt);
public sealed record ActionDecisionStateResponse(Guid OpportunityId, string CurrentStatus, uint Version, IReadOnlyList<ActionDecisionItem> Decisions);

public static class ActionDecisionPolicy
{
    public static bool CanTransition(string currentStatus, string nextStatus, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (expiresAt <= now && currentStatus == OpportunityStatuses.Available) return false;
        if (ActionStatuses.IsTerminal(currentStatus) || currentStatus == OpportunityStatuses.Expired) return false;
        return currentStatus switch
        {
            OpportunityStatuses.Available => nextStatus is ActionStatuses.Applied or ActionStatuses.Skipped or ActionStatuses.NotRelevant or ActionStatuses.Rejected,
            ActionStatuses.Applied => nextStatus is ActionStatuses.Completed or ActionStatuses.Skipped or ActionStatuses.NotRelevant or ActionStatuses.Rejected,
            _ => false
        };
    }
}

public static class ActionDecisionEndpoints
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

    private static async Task<ActionDecisionStateResponse> State(Opportunity opportunity, AtlasDbContext db, CancellationToken ct)
    {
        var decisions = await db.ActionDecisionRecords
            .Where(x => x.BusinessId == opportunity.BusinessId && x.OpportunityId == opportunity.Id)
            .OrderBy(x => x.DecidedAt)
            .ThenBy(x => x.Id)
            .Select(x => new ActionDecisionItem(x.Id, x.Status, x.ReasonCode, x.OwnerNote, x.DecidedAt))
            .ToListAsync(ct);
        return new ActionDecisionStateResponse(opportunity.Id, OpportunityPolicy.StatusFor(opportunity, DateTimeOffset.UtcNow), opportunity.ConcurrencyVersion, decisions);
    }

    public static IEndpointRouteBuilder MapActionDecisionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/businesses/{businessId:guid}/opportunities/{opportunityId:guid}/action-decisions", async (
            Guid businessId, Guid opportunityId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            if (await OwnerAccount(businessId, user, db, ct) is null) return Results.NotFound();
            var opportunity = await db.Set<Opportunity>().SingleOrDefaultAsync(x => x.Id == opportunityId && x.BusinessId == businessId, ct);
            return opportunity is null ? Results.NotFound() : Results.Ok(await State(opportunity, db, ct));
        }).RequireAuthorization("BusinessOwner");

        app.MapPost("/api/v1/businesses/{businessId:guid}/opportunities/{opportunityId:guid}/action-decisions", async (
            Guid businessId, Guid opportunityId, RecordActionDecisionRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();

            var errors = request.Validate();
            if (errors.Count > 0)
                return Results.ValidationProblem(errors, extensions: new Dictionary<string, object?> { ["code"] = "action_decision_invalid" });

            var opportunity = await db.Set<Opportunity>().SingleOrDefaultAsync(x => x.Id == opportunityId && x.BusinessId == businessId, ct);
            if (opportunity is null) return Results.NotFound();
            if (opportunity.ConcurrencyVersion != request.Version)
                return Results.Conflict(new { code = "action_decision_stale", message = "The Action changed. Refresh before recording another decision." });

            var currentStatus = OpportunityPolicy.StatusFor(opportunity, DateTimeOffset.UtcNow);
            if (!ActionDecisionPolicy.CanTransition(currentStatus, request.Status, opportunity.ExpiresAt, DateTimeOffset.UtcNow))
                return Results.Conflict(new { code = "action_transition_invalid", message = "That Action status transition is not allowed." });

            var record = new ActionDecisionRecord
            {
                Id = Guid.NewGuid(), BusinessId = businessId, OpportunityId = opportunity.Id, GoalId = opportunity.GoalId,
                KnowledgePackVersionId = opportunity.KnowledgePackVersionId, KnowledgePackKey = opportunity.KnowledgePackKey,
                KnowledgePackVersion = opportunity.KnowledgePackVersion, Status = request.Status,
                ReasonCode = request.ReasonCode?.Trim(), OwnerNote = request.OwnerNote?.Trim(),
                DecidedByUserAccountId = account.Id, DecidedAt = DateTimeOffset.UtcNow,
                OpportunityVersionBeforeDecision = opportunity.ConcurrencyVersion
            };

            opportunity.Status = request.Status;
            opportunity.DecidedAt = record.DecidedAt;
            opportunity.DecidedByUserAccountId = account.Id;
            opportunity.DecisionReason = record.ReasonCode;
            db.ActionDecisionRecords.Add(record);
            db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"action.decision:{opportunity.Id}:{request.Status}:{record.ReasonCode ?? "none"}"));
            await db.SaveChangesAsync(ct);
            return Results.Ok(await State(opportunity, db, ct));
        }).RequireAuthorization("BusinessOwner");

        return app;
    }
}
