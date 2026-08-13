using System.Security.Claims;
using System.Text;
using System.Text.Json;
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

public sealed record OpportunityEvidenceItem(string Category, string Label, string Value, string Source);
public sealed record OpportunityDetailResponse(
    Guid Id,
    string Title,
    string Status,
    string GoalAlignment,
    string? GoalTitle,
    string Reason,
    string WhyNow,
    string Confidence,
    string ExpectedImpact,
    string Effort,
    IReadOnlyList<OpportunityEvidenceItem> Evidence,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<string> SourceCategories,
    string ActionSummary,
    bool ExecutionKitAvailable,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsExpired,
    string KnowledgePackKey,
    string KnowledgePackVersion,
    uint Version);

public static class OpportunityPolicy
{
    private static readonly string[] LegacyAssumptions =
    [
        "The owner-confirmed profile and selected goal remain accurate.",
        "The recorded Knowledge Pack version is applicable to this Business."
    ];

    private static readonly string[] LegacyLimitations =
    [
        "Expected impact is directional, not guaranteed.",
        "Atlas has not measured an outcome yet.",
        "External action still requires owner review."
    ];

    private sealed record ParsedEvidence(
        IReadOnlyList<OpportunityEvidenceItem> Evidence,
        string GoalAlignment,
        string? GoalTitle,
        IReadOnlyList<string> Assumptions,
        IReadOnlyList<string> Limitations);

    public static bool IsEligible(BusinessProfile? profile, IReadOnlyCollection<BusinessGoal> goals, BusinessKnowledgeAssignment? assignment) =>
        profile is { OwnerConfirmed: true } && goals.Count > 0 && assignment is { IsCurrent: true };

    public static string StatusFor(Opportunity value, DateTimeOffset now) =>
        value.Status == OpportunityStatuses.Available && value.ExpiresAt <= now ? OpportunityStatuses.Expired : value.Status;

    public static bool CanDecide(Opportunity value, DateTimeOffset now) =>
        value.Status == OpportunityStatuses.Available && value.ExpiresAt > now;

    public static OpportunityDetailResponse Detail(Opportunity value, BusinessGoal? goal, DateTimeOffset now)
    {
        var parsed = ParseEvidence(value, goal);
        var expired = value.ExpiresAt <= now;
        return new OpportunityDetailResponse(
            value.Id,
            value.Title,
            StatusFor(value, now),
            parsed.GoalAlignment,
            parsed.GoalTitle,
            value.WhyItMatters,
            value.WhyNow,
            value.Confidence,
            value.ExpectedImpact,
            value.Effort,
            parsed.Evidence,
            parsed.Assumptions,
            parsed.Limitations,
            parsed.Evidence.Select(x => x.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            $"Review and apply the proposed action: {value.Title}",
            ExecutionKitPolicy.IsEligible(value, now),
            value.CreatedAt,
            value.ExpiresAt,
            expired,
            value.KnowledgePackKey,
            value.KnowledgePackVersion,
            value.ConcurrencyVersion);
    }

    private static ParsedEvidence ParseEvidence(Opportunity value, BusinessGoal? goal)
    {
        var fallbackAlignment = goal is null
            ? "This Opportunity references a goal that is no longer available."
            : $"Aligned to priority #{goal.Priority}: {goal.Title}";
        var fallbackTitle = goal?.Title;

        try
        {
            using var document = JsonDocument.Parse(value.EvidenceJson);
            var root = document.RootElement;

            if (IsVs23Snapshot(root))
                return ParseVs23Snapshot(root, value, fallbackAlignment, fallbackTitle);

            return ParseLegacySnapshot(root, value, fallbackAlignment, fallbackTitle);
        }
        catch (JsonException)
        {
            return Fallback(value, fallbackAlignment, fallbackTitle);
        }
        catch (InvalidOperationException)
        {
            return Fallback(value, fallbackAlignment, fallbackTitle);
        }
    }

    private static bool IsVs23Snapshot(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty("schemaVersion", out var schemaVersion) &&
        schemaVersion.ValueKind == JsonValueKind.Number &&
        schemaVersion.TryGetInt32(out var version) &&
        version is >= 1 && version <= OpportunityGenerationSnapshot.SchemaVersion &&
        root.TryGetProperty("evidence", out var evidence) && evidence.ValueKind == JsonValueKind.Array;

    private static ParsedEvidence ParseVs23Snapshot(
        JsonElement root,
        Opportunity value,
        string fallbackAlignment,
        string? fallbackTitle)
    {
        var alignment = fallbackAlignment;
        var title = fallbackTitle;
        if (root.TryGetProperty("goal", out var goalValue) && goalValue.ValueKind == JsonValueKind.Object)
        {
            alignment = ReadString(goalValue, "alignment") ?? alignment;
            title = ReadString(goalValue, "title") ?? title;
        }

        var evidence = new List<OpportunityEvidenceItem>();
        var evidenceArray = root.GetProperty("evidence");
        foreach (var item in evidenceArray.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var layer = ReadString(item, "layer");
            var key = ReadString(item, "key");
            var itemValue = ReadString(item, "value");
            var source = ReadString(item, "source");
            if (string.IsNullOrWhiteSpace(layer) || string.IsNullOrWhiteSpace(key) ||
                string.IsNullOrWhiteSpace(itemValue) || string.IsNullOrWhiteSpace(source)) continue;

            evidence.Add(new OpportunityEvidenceItem(layer, EvidenceLabel(key), itemValue, source));
        }

        if (evidence.Count == 0)
            return Fallback(value, alignment, title);

        return new ParsedEvidence(
            evidence,
            alignment,
            title,
            ReadStringArray(root, "assumptions", LegacyAssumptions),
            ReadStringArray(root, "limitations", LegacyLimitations));
    }

    private static ParsedEvidence ParseLegacySnapshot(
        JsonElement root,
        Opportunity value,
        string fallbackAlignment,
        string? fallbackTitle)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return Fallback(value, fallbackAlignment, fallbackTitle);

        var evidence = new List<OpportunityEvidenceItem>();
        if (root.TryGetProperty("profile", out var profile) && profile.ValueKind == JsonValueKind.String)
            evidence.Add(new("business-profile", "Business Profile", profile.GetString() ?? "confirmed", "owner-confirmed"));

        if (root.TryGetProperty("goal", out var goalValue) && goalValue.ValueKind == JsonValueKind.String)
            evidence.Add(new("business-goal", "Priority goal", goalValue.GetString() ?? fallbackTitle ?? "Selected goal", "owner-selected"));

        var packKey = ReadString(root, "PackKey") ?? ReadString(root, "packKey");
        if (!string.IsNullOrWhiteSpace(packKey))
            evidence.Add(new("knowledge-pack", "Knowledge Pack", $"{packKey} v{value.KnowledgePackVersion}", "published-pack"));

        if (evidence.Count == 0)
            return Fallback(value, fallbackAlignment, fallbackTitle);

        return new ParsedEvidence(evidence, fallbackAlignment, fallbackTitle, LegacyAssumptions, LegacyLimitations);
    }

    private static ParsedEvidence Fallback(Opportunity value, string alignment, string? goalTitle) => new(
        [new OpportunityEvidenceItem("summary", "Evidence summary", value.EvidenceSummary, "recorded-evidence")],
        alignment,
        goalTitle,
        LegacyAssumptions,
        LegacyLimitations);

    private static string? ReadString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String) return null;
        var result = value.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string property, IReadOnlyList<string> fallback)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array) return fallback;
        return value.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();
    }

    private static string EvidenceLabel(string key)
    {
        var canonical = key.Trim();
        if (canonical.Equals("primarychannels", StringComparison.OrdinalIgnoreCase)) return "Primary channels";
        if (canonical.Equals("currentpriorities", StringComparison.OrdinalIgnoreCase)) return "Current priorities";
        if (canonical.Equals("businessHours", StringComparison.OrdinalIgnoreCase)) return "Business hours";
        if (canonical.Equals("openingHours", StringComparison.OrdinalIgnoreCase)) return "Opening hours";
        if (canonical.Equals("reviewSignal", StringComparison.OrdinalIgnoreCase)) return "Review signal";
        if (canonical.Equals("reputationSignal", StringComparison.OrdinalIgnoreCase)) return "Reputation signal";

        var builder = new StringBuilder(canonical.Length + 8);
        for (var index = 0; index < canonical.Length; index++)
        {
            var current = canonical[index];
            if (current is '-' or '_')
            {
                if (builder.Length > 0 && builder[^1] != ' ') builder.Append(' ');
                continue;
            }
            if (index > 0 && char.IsUpper(current) && builder.Length > 0 && builder[^1] != ' ') builder.Append(' ');
            builder.Append(char.ToLowerInvariant(current));
        }
        var result = builder.ToString().Trim();
        return result.Length == 0 ? "Evidence" : char.ToUpperInvariant(result[0]) + result[1..];
    }
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

    public static IEndpointRouteBuilder MapOpportunityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/businesses/{businessId:guid}/today-focus", async (Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();

            var result = await OpportunityFocusService.GenerateAsync(db, businessId, account.Id, DateTimeOffset.UtcNow, ct);
            return result.State switch
            {
                OpportunityFocusGenerationStates.Ready when result.Opportunity is not null =>
                    Results.Ok(new { state = OpportunityFocusGenerationStates.Ready, opportunity = TodayFocusResponse.From(result.Opportunity) }),
                OpportunityFocusGenerationStates.InsufficientContext =>
                    Results.Ok(new { state = OpportunityFocusGenerationStates.InsufficientContext, code = result.Code, message = result.Message }),
                OpportunityFocusGenerationStates.NoFocus =>
                    Results.Ok(new { state = OpportunityFocusGenerationStates.NoFocus, code = result.Code, message = result.Message }),
                _ => Results.Ok(new { state = OpportunityFocusGenerationStates.Degraded, code = result.Code, message = result.Message })
            };
        }).RequireAuthorization("BusinessOwner");

        app.MapGet("/api/v1/businesses/{businessId:guid}/opportunities/{opportunityId:guid}", async (Guid businessId, Guid opportunityId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            if (await OwnerAccount(businessId, user, db, ct) is null) return Results.NotFound();
            var opportunity = await db.Set<Opportunity>().SingleOrDefaultAsync(x => x.Id == opportunityId && x.BusinessId == businessId, ct);
            if (opportunity is null) return Results.NotFound();
            var goal = opportunity.GoalId is null ? null : await db.BusinessGoals.SingleOrDefaultAsync(x => x.Id == opportunity.GoalId && x.BusinessId == businessId, ct);
            return Results.Ok(OpportunityPolicy.Detail(opportunity, goal, DateTimeOffset.UtcNow));
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
