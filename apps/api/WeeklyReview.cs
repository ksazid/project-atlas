using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed record WeeklyReviewCounts(
    int Opportunities,
    int Applied,
    int Completed,
    int Skipped,
    int NotRelevant,
    int Rejected,
    int OutcomesRecorded,
    int OutcomesMissing,
    int ExecutionAssetsUsed);

public sealed record WeeklyReviewOutcomeItem(
    Guid OpportunityId,
    string OpportunityTitle,
    string Status,
    string? GoalTitle,
    string ResultSummary,
    string EvidenceClass,
    int UsefulnessRating,
    string KnowledgePackKey,
    string KnowledgePackVersion,
    DateTimeOffset RecordedAt);

public sealed record WeeklyReviewOpenItem(
    Guid OpportunityId,
    string OpportunityTitle,
    string Status,
    string? GoalTitle,
    string KnowledgePackKey,
    string KnowledgePackVersion,
    DateTimeOffset LastActivityAt);

public sealed record WeeklyReviewResponse(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    WeeklyReviewCounts Counts,
    IReadOnlyList<WeeklyReviewOutcomeItem> Outcomes,
    IReadOnlyList<WeeklyReviewOpenItem> OpenItems,
    IReadOnlyList<string> Highlights,
    string EvidenceNote);

public static class WeeklyReviewPolicy
{
    public static (DateTimeOffset Start, DateTimeOffset End) Window(DateTimeOffset end)
    {
        var normalizedEnd = end.ToUniversalTime();
        return (normalizedEnd.AddDays(-7), normalizedEnd);
    }

    public static bool InWindow(DateTimeOffset value, DateTimeOffset start, DateTimeOffset end) =>
        value >= start && value <= end;

    public static IReadOnlyList<string> Highlights(WeeklyReviewCounts counts)
    {
        var values = new List<string>();
        if (counts.Completed > 0) values.Add($"{counts.Completed} Action{(counts.Completed == 1 ? "" : "s")} completed during this review period.");
        if (counts.OutcomesRecorded > 0) values.Add($"{counts.OutcomesRecorded} completed Action{(counts.OutcomesRecorded == 1 ? " has" : "s have")} a recorded Outcome.");
        if (counts.OutcomesMissing > 0) values.Add($"{counts.OutcomesMissing} completed Action{(counts.OutcomesMissing == 1 ? " is" : "s are")} still missing an Outcome.");
        if (counts.ExecutionAssetsUsed > 0) values.Add($"{counts.ExecutionAssetsUsed} Execution Kit asset{(counts.ExecutionAssetsUsed == 1 ? " was" : "s were")} marked used.");
        if (values.Count == 0) values.Add("No recorded Action or Outcome activity was found for this seven-day period.");
        return values;
    }
}

public static class WeeklyReviewEndpoints
{
    private static string? Subject(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

    private static async Task<bool> IsOwner(Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        var subject = Subject(user);
        if (string.IsNullOrWhiteSpace(subject)) return false;
        return await db.BusinessMemberships.AnyAsync(
            x => x.BusinessId == businessId && x.UserAccount.ProviderSubject == subject && x.Role == MembershipRoles.BusinessOwner, ct);
    }

    public static IEndpointRouteBuilder MapWeeklyReviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/businesses/{businessId:guid}/weekly-review", async (
            Guid businessId,
            DateTimeOffset? endingAt,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            if (!await IsOwner(businessId, user, db, ct)) return Results.NotFound();

            var requestedEnd = endingAt ?? DateTimeOffset.UtcNow;
            if (requestedEnd > DateTimeOffset.UtcNow.AddMinutes(5))
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(endingAt)] = ["Weekly review end time cannot be in the future."] });

            var (periodStart, periodEnd) = WeeklyReviewPolicy.Window(requestedEnd);

            var decisions = await db.ActionDecisionRecords.AsNoTracking()
                .Where(x => x.BusinessId == businessId && x.DecidedAt >= periodStart && x.DecidedAt <= periodEnd)
                .OrderBy(x => x.DecidedAt)
                .ToListAsync(ct);

            var outcomes = await db.Outcomes.AsNoTracking()
                .Where(x => x.BusinessId == businessId && x.UpdatedAt >= periodStart && x.UpdatedAt <= periodEnd)
                .ToListAsync(ct);

            var createdOpportunityIds = await db.Set<Opportunity>().AsNoTracking()
                .Where(x => x.BusinessId == businessId && x.CreatedAt >= periodStart && x.CreatedAt <= periodEnd)
                .Select(x => x.Id)
                .ToListAsync(ct);

            var opportunityIds = createdOpportunityIds
                .Concat(decisions.Select(x => x.OpportunityId))
                .Concat(outcomes.Select(x => x.OpportunityId))
                .Distinct()
                .ToArray();

            if (opportunityIds.Length == 0)
            {
                var emptyCounts = new WeeklyReviewCounts(0, 0, 0, 0, 0, 0, 0, 0, 0);
                return Results.Ok(new WeeklyReviewResponse(
                    periodStart, periodEnd, emptyCounts, [], [], WeeklyReviewPolicy.Highlights(emptyCounts),
                    "Weekly Review summarizes recorded Atlas activity only. It does not infer unrecorded work or causal business impact."));
            }

            var opportunities = await db.Set<Opportunity>().AsNoTracking()
                .Where(x => x.BusinessId == businessId && opportunityIds.Contains(x.Id))
                .ToListAsync(ct);

            var goalIds = opportunities.Where(x => x.GoalId.HasValue).Select(x => x.GoalId!.Value).Distinct().ToArray();
            var goals = await db.BusinessGoals.AsNoTracking()
                .Where(x => x.BusinessId == businessId && goalIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

            var kits = await db.ExecutionKits.AsNoTracking().Include(x => x.Assets)
                .Where(x => x.BusinessId == businessId && opportunityIds.Contains(x.OpportunityId))
                .ToListAsync(ct);

            var allOutcomes = await db.Outcomes.AsNoTracking()
                .Where(x => x.BusinessId == businessId && opportunityIds.Contains(x.OpportunityId))
                .ToDictionaryAsync(x => x.OpportunityId, ct);

            var latestDecisionByOpportunity = decisions
                .GroupBy(x => x.OpportunityId)
                .ToDictionary(x => x.Key, x => x.OrderBy(d => d.DecidedAt).ThenBy(d => d.Id).Last());

            var completedIds = latestDecisionByOpportunity.Values
                .Where(x => x.Status == ActionStatuses.Completed)
                .Select(x => x.OpportunityId)
                .ToHashSet();

            var counts = new WeeklyReviewCounts(
                Opportunities: opportunities.Count,
                Applied: decisions.Count(x => x.Status == ActionStatuses.Applied),
                Completed: decisions.Count(x => x.Status == ActionStatuses.Completed),
                Skipped: decisions.Count(x => x.Status == ActionStatuses.Skipped),
                NotRelevant: decisions.Count(x => x.Status == ActionStatuses.NotRelevant),
                Rejected: decisions.Count(x => x.Status == ActionStatuses.Rejected),
                OutcomesRecorded: completedIds.Count(id => allOutcomes.ContainsKey(id)),
                OutcomesMissing: completedIds.Count(id => !allOutcomes.ContainsKey(id)),
                ExecutionAssetsUsed: kits.Sum(x => x.Assets.Count(a => a.IsUsed && a.UpdatedAt >= periodStart && a.UpdatedAt <= periodEnd)));

            var opportunityById = opportunities.ToDictionary(x => x.Id);
            var outcomeItems = outcomes
                .Where(x => opportunityById.ContainsKey(x.OpportunityId))
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x =>
                {
                    var opportunity = opportunityById[x.OpportunityId];
                    BusinessGoal? goal = null;
                    if (opportunity.GoalId.HasValue) goals.TryGetValue(opportunity.GoalId.Value, out goal);
                    return new WeeklyReviewOutcomeItem(
                        opportunity.Id,
                        opportunity.Title,
                        OpportunityPolicy.StatusFor(opportunity, periodEnd),
                        goal?.Title,
                        x.ResultSummary,
                        x.EvidenceClass,
                        x.UsefulnessRating,
                        x.KnowledgePackKey,
                        x.KnowledgePackVersion,
                        x.UpdatedAt);
                })
                .ToList();

            var openItems = opportunities
                .Select(x =>
                {
                    latestDecisionByOpportunity.TryGetValue(x.Id, out var decision);
                    var status = decision?.Status ?? OpportunityPolicy.StatusFor(x, periodEnd);
                    BusinessGoal? goal = null;
                    if (x.GoalId.HasValue) goals.TryGetValue(x.GoalId.Value, out goal);
                    var lastActivity = decision?.DecidedAt ?? x.DecidedAt ?? x.CreatedAt;
                    return new WeeklyReviewOpenItem(
                        x.Id, x.Title, status, goal?.Title, x.KnowledgePackKey, x.KnowledgePackVersion, lastActivity);
                })
                .Where(x => x.Status is OpportunityStatuses.Available or ActionStatuses.Applied)
                .OrderByDescending(x => x.LastActivityAt)
                .Take(10)
                .ToList();

            return Results.Ok(new WeeklyReviewResponse(
                periodStart,
                periodEnd,
                counts,
                outcomeItems,
                openItems,
                WeeklyReviewPolicy.Highlights(counts),
                "Weekly Review summarizes recorded Atlas activity only. Outcome statements retain their recorded evidence class; Atlas does not infer causal ROI or unrecorded work."));
        }).RequireAuthorization("BusinessOwner");

        return app;
    }
}
