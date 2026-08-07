using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed record HistoryExecutionKitSummary(Guid Id, string Status, int AssetCount, int UsedAssetCount, int? UsefulnessRating);
public sealed record HistoryOutcomeSummary(Guid Id, string ResultSummary, string EvidenceClass, int UsefulnessRating, DateTimeOffset UpdatedAt);
public sealed record HistoryItemResponse(
    Guid OpportunityId,
    string Title,
    string Status,
    Guid? GoalId,
    string? GoalTitle,
    IReadOnlyList<string> Categories,
    string? DecisionReasonCode,
    string? DecisionOwnerNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastActionAt,
    HistoryExecutionKitSummary? ExecutionKit,
    HistoryOutcomeSummary? Outcome,
    string LearningSummary,
    string KnowledgePackKey,
    string KnowledgePackVersion);

public sealed record HistoryResponse(IReadOnlyList<HistoryItemResponse> Items, int Count);

public static class HistoryPolicy
{
    public static IReadOnlyList<string> Categories(Opportunity opportunity)
    {
        var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var document = JsonDocument.Parse(opportunity.EvidenceJson);
            var root = document.RootElement;
            if (root.TryGetProperty("profile", out _)) categories.Add("business-profile");
            if (root.TryGetProperty("goal", out _)) categories.Add("business-goal");
            if (root.TryGetProperty("PackKey", out _)) categories.Add("knowledge-pack");
        }
        catch (JsonException)
        {
            categories.Add("recorded-evidence");
        }

        if (categories.Count == 0) categories.Add("recorded-evidence");
        return categories.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static bool Matches(string currentStatus, IReadOnlyCollection<string> categories, string? status, string? category)
    {
        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(currentStatus, status.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(category) && !categories.Contains(category.Trim(), StringComparer.OrdinalIgnoreCase)) return false;
        return true;
    }

    public static int ClampLimit(int? requested) => Math.Clamp(requested ?? 50, 1, 100);

    public static string LearningSummary(string status, ActionDecisionRecord? decision, Outcome? outcome)
    {
        if (outcome is not null) return $"{outcome.EvidenceClass} outcome recorded: {outcome.ResultSummary}";
        if (decision is not null && !string.IsNullOrWhiteSpace(decision.ReasonCode)) return $"Action status {status}; owner reason: {decision.ReasonCode}.";
        return $"Action status: {status}. No outcome has been recorded yet.";
    }
}

public static class HistoryEndpoints
{
    private static string? Subject(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

    private static async Task<bool> IsOwner(Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        var subject = Subject(user);
        if (string.IsNullOrWhiteSpace(subject)) return false;
        return await db.BusinessMemberships.AnyAsync(
            x => x.BusinessId == businessId && x.UserAccount.ProviderSubject == subject && x.Role == MembershipRoles.BusinessOwner, ct);
    }

    public static IEndpointRouteBuilder MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/businesses/{businessId:guid}/history", async (
            Guid businessId,
            string? status,
            string? category,
            Guid? goalId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? limit,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            if (!await IsOwner(businessId, user, db, ct)) return Results.NotFound();
            if (from.HasValue && to.HasValue && from.Value > to.Value)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["From date must be before or equal to To date."] });

            var query = db.Set<Opportunity>().AsNoTracking().Where(x => x.BusinessId == businessId);
            if (goalId.HasValue) query = query.Where(x => x.GoalId == goalId.Value);
            if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value);

            var opportunities = await query.OrderByDescending(x => x.CreatedAt).Take(250).ToListAsync(ct);
            if (opportunities.Count == 0) return Results.Ok(new HistoryResponse([], 0));

            var opportunityIds = opportunities.Select(x => x.Id).ToArray();
            var goalIds = opportunities.Where(x => x.GoalId.HasValue).Select(x => x.GoalId!.Value).Distinct().ToArray();

            var goals = await db.BusinessGoals.AsNoTracking()
                .Where(x => x.BusinessId == businessId && goalIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);
            var decisions = await db.ActionDecisionRecords.AsNoTracking()
                .Where(x => x.BusinessId == businessId && opportunityIds.Contains(x.OpportunityId))
                .OrderBy(x => x.DecidedAt)
                .ToListAsync(ct);
            var latestDecisions = decisions.GroupBy(x => x.OpportunityId).ToDictionary(x => x.Key, x => x.Last());
            var kits = await db.ExecutionKits.AsNoTracking().Include(x => x.Assets)
                .Where(x => x.BusinessId == businessId && opportunityIds.Contains(x.OpportunityId))
                .ToDictionaryAsync(x => x.OpportunityId, ct);
            var outcomes = await db.Outcomes.AsNoTracking()
                .Where(x => x.BusinessId == businessId && opportunityIds.Contains(x.OpportunityId))
                .ToDictionaryAsync(x => x.OpportunityId, ct);

            var now = DateTimeOffset.UtcNow;
            var items = new List<HistoryItemResponse>();
            foreach (var opportunity in opportunities)
            {
                var currentStatus = OpportunityPolicy.StatusFor(opportunity, now);
                var categories = HistoryPolicy.Categories(opportunity);
                if (!HistoryPolicy.Matches(currentStatus, categories, status, category)) continue;

                latestDecisions.TryGetValue(opportunity.Id, out var decision);
                kits.TryGetValue(opportunity.Id, out var kit);
                outcomes.TryGetValue(opportunity.Id, out var outcome);
                BusinessGoal? goal = null;
                if (opportunity.GoalId.HasValue) goals.TryGetValue(opportunity.GoalId.Value, out goal);

                HistoryExecutionKitSummary? kitSummary = null;
                if (kit is not null)
                {
                    var ratings = kit.Assets.Where(x => x.UsefulnessRating.HasValue).Select(x => x.UsefulnessRating!.Value).ToArray();
                    kitSummary = new HistoryExecutionKitSummary(
                        kit.Id, kit.Status, kit.Assets.Count, kit.Assets.Count(x => x.IsUsed),
                        ratings.Length == 0 ? null : (int)Math.Round(ratings.Average()));
                }

                var outcomeSummary = outcome is null
                    ? null
                    : new HistoryOutcomeSummary(outcome.Id, outcome.ResultSummary, outcome.EvidenceClass, outcome.UsefulnessRating, outcome.UpdatedAt);

                items.Add(new HistoryItemResponse(
                    opportunity.Id,
                    opportunity.Title,
                    currentStatus,
                    opportunity.GoalId,
                    goal?.Title,
                    categories,
                    decision?.ReasonCode,
                    decision?.OwnerNote,
                    opportunity.CreatedAt,
                    opportunity.ExpiresAt,
                    decision?.DecidedAt ?? opportunity.DecidedAt,
                    kitSummary,
                    outcomeSummary,
                    HistoryPolicy.LearningSummary(currentStatus, decision, outcome),
                    opportunity.KnowledgePackKey,
                    opportunity.KnowledgePackVersion));
            }

            var limited = items.Take(HistoryPolicy.ClampLimit(limit)).ToList();
            return Results.Ok(new HistoryResponse(limited, limited.Count));
        }).RequireAuthorization("BusinessOwner");

        return app;
    }
}
