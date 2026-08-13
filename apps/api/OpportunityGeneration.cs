using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Atlas.Api;

public sealed record OpportunityEvidenceReference(
    string EvidenceId,
    string Layer,
    string Key,
    string Value,
    string Source);

public sealed record GeneratedOpportunityCandidate(
    string PatternKey,
    string Title,
    Guid GoalId,
    string GoalType,
    string GoalTitle,
    int GoalPriority,
    string GoalAlignment,
    string Reason,
    string WhyNow,
    string ExpectedImpact,
    string Effort,
    string Confidence,
    IReadOnlyList<OpportunityEvidenceReference> Evidence,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Limitations,
    string ExecutionTemplateKey,
    int CooldownDays,
    string KnowledgePackKey,
    string KnowledgePackVersion,
    IReadOnlyList<ResolvedKnowledgeManifest> Manifests,
    string BundleFingerprint,
    string CooldownFingerprint,
    DateTimeOffset GeneratedAt,
    DateTimeOffset ExpiresAt,
    bool CategorySpecific);

public sealed record OpportunityGenerationResult(
    IReadOnlyList<GeneratedOpportunityCandidate> Candidates,
    GeneratedOpportunityCandidate? Selected);

public static class OpportunityGenerator
{
    private static readonly HashSet<string> OrderingChannelKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "operatingchannels", "primarychannels", "orderingChannel", "orderingChannels", "primaryOrderingChannel", "serviceChannel", "serviceChannels"
    };

    private static readonly HashSet<string> HoursKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "businessHours", "openingHours", "hours", "operatingHours"
    };

    private static readonly HashSet<string> OfferKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "currentpriorities", "currentOffer", "promotion", "currentPromotion", "nearTermPriority", "commercialPriority"
    };

    private static readonly HashSet<string> ReputationKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "reputationSignal", "reviewSignal", "ratingSignal", "reputationConcern", "reviewConcern"
    };

    public static OpportunityGenerationResult Generate(
        BusinessProfile? profile,
        IReadOnlyCollection<BusinessGoal> goals,
        ResolvedKnowledgeBundle bundle,
        IReadOnlyCollection<Opportunity> priorOpportunities,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(goals);
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(priorOpportunities);

        if (profile is not { OwnerConfirmed: true } || goals.Count == 0)
            return new OpportunityGenerationResult([], null);

        var manifests = ResolvePackagedManifests(bundle);
        if (manifests.Count == 0) return new OpportunityGenerationResult([], null);

        var candidates = new List<GeneratedOpportunityCandidate>();
        foreach (var manifest in manifests)
        {
            foreach (var pattern in manifest.OpportunityPatterns)
            {
                foreach (var goal in goals.OrderBy(x => x.Priority).ThenBy(x => x.Id))
                {
                    if (!MatchesGoal(pattern, goal)) continue;
                    if (!TryResolveEvidence(manifest, pattern, profile, goal, bundle, out var evidence)) continue;

                    var cooldownFingerprint = OpportunityGenerationSnapshot.ComputeCooldownFingerprint(
                        pattern.Key,
                        goal.Id,
                        goal.Type,
                        goal.Title,
                        goal.Priority,
                        evidence.Select(x => x.EvidenceId));
                    if (IsSuppressed(pattern.Key, pattern.CooldownDays, cooldownFingerprint, priorOpportunities, now)) continue;

                    candidates.Add(CreateCandidate(manifest, pattern, goal, bundle, evidence, cooldownFingerprint, now));
                }
            }
        }

        var ordered = candidates
            .OrderBy(x => x.GoalPriority)
            .ThenBy(OperationalPriorityRank)
            .ThenByDescending(OperationalMateriality)
            .ThenBy(x => ConfidenceRank(x.Confidence))
            .ThenBy(x => EffortRank(x.Effort))
            .ThenBy(x => x.CategorySpecific ? 0 : 1)
            .ThenBy(x => x.PatternKey, StringComparer.Ordinal)
            .ThenBy(x => x.GoalId)
            .ToArray();

        return new OpportunityGenerationResult(ordered, ordered.FirstOrDefault());
    }

    private static IReadOnlyList<KnowledgePackManifestV2> ResolvePackagedManifests(ResolvedKnowledgeBundle bundle)
    {
        var result = new List<KnowledgePackManifestV2>();
        var core = GenericBusinessKnowledgeManifestV2.Create();
        if (HasExactManifest(bundle, core)) result.Add(core);

        if (string.Equals(bundle.CategoryKey, "restaurant-cafe", StringComparison.OrdinalIgnoreCase))
        {
            var restaurant = RestaurantCafeKnowledgeManifestV2.Create();
            if (HasExactManifest(bundle, restaurant)) result.Add(restaurant);
        }

        return result;
    }

    private static bool HasExactManifest(ResolvedKnowledgeBundle bundle, KnowledgePackManifestV2 manifest) =>
        bundle.Manifests.Any(x =>
            string.Equals(x.Layer, manifest.Layer, StringComparison.Ordinal) &&
            string.Equals(x.PackKey, manifest.PackKey, StringComparison.Ordinal) &&
            string.Equals(x.ExactVersion, manifest.ExactVersion, StringComparison.Ordinal));

    private static bool MatchesGoal(KnowledgeOpportunityPattern pattern, BusinessGoal goal)
    {
        var ownerType = goal.Type.Trim();
        if (pattern.GoalTypes.Any(candidate => string.Equals(candidate, ownerType, StringComparison.OrdinalIgnoreCase)))
            return true;

        var canonicalIntent = ownerType.ToLowerInvariant() switch
        {
            "revenue" or "acquisition" => "growth",
            "saved-time" or "reduced-waste" or "operational-consistency" => "efficiency",
            "reputation" when string.Equals(pattern.Key, "reputation-signal-follow-up", StringComparison.Ordinal) => "customer-experience",
            _ => null
        };

        return canonicalIntent is not null &&
            pattern.GoalTypes.Any(candidate => string.Equals(candidate, canonicalIntent, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryResolveEvidence(
        KnowledgePackManifestV2 manifest,
        KnowledgeOpportunityPattern pattern,
        BusinessProfile profile,
        BusinessGoal goal,
        ResolvedKnowledgeBundle bundle,
        out IReadOnlyList<OpportunityEvidenceReference> evidence)
    {
        var resolved = new List<OpportunityEvidenceReference>();
        foreach (var ruleKey in pattern.EvidenceRuleKeys)
        {
            var rule = manifest.EvidenceRules.SingleOrDefault(x => string.Equals(x.Key, ruleKey, StringComparison.Ordinal));
            if (rule is null || !TryResolveRule(rule, profile, goal, bundle, out var ruleEvidence))
            {
                evidence = [];
                return false;
            }

            if (ruleEvidence.Count < rule.MinimumEvidenceCount)
            {
                evidence = [];
                return false;
            }
            resolved.AddRange(ruleEvidence);
        }

        evidence = resolved
            .GroupBy(x => x.EvidenceId, StringComparer.Ordinal)
            .Select(x => x.First())
            .OrderBy(x => x.Layer, StringComparer.Ordinal)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.EvidenceId, StringComparer.Ordinal)
            .ToArray();
        return true;
    }

    private static bool TryResolveRule(
        KnowledgeEvidenceRule rule,
        BusinessProfile profile,
        BusinessGoal goal,
        ResolvedKnowledgeBundle bundle,
        out IReadOnlyList<OpportunityEvidenceReference> evidence)
    {
        if (rule.OperationalRequirement is { } requirement)
        {
            evidence = OperationalEvidenceMatcher.Match(requirement, bundle.OperationalFacts)
                .Select(FactEvidence)
                .ToArray();
            return evidence.Count >= rule.MinimumEvidenceCount;
        }

        switch (rule.Key)
        {
            case "confirmed-profile":
                evidence = [PolicyEvidence("confirmed-profile", "owner-confirmed", profile.Source)];
                return profile.OwnerConfirmed;

            case "priority-goal":
                evidence = [PolicyEvidence("priority-goal", $"{goal.Priority}:{goal.Type}:{goal.Title}", FieldSources.Owner)];
                return true;

            case "restaurant-category-confirmed":
                if (!string.Equals(bundle.CategoryKey, "restaurant-cafe", StringComparison.OrdinalIgnoreCase) ||
                    !bundle.Manifests.Any(x => x.PackKey == RestaurantCafeKnowledgeManifestV2.PackKey && x.ExactVersion == RestaurantCafeKnowledgeManifestV2.Version))
                {
                    evidence = [];
                    return false;
                }
                evidence = [PolicyEvidence("restaurant-category-confirmed", bundle.CategoryKey, FieldSources.Owner)];
                return true;

            case "ordering-channel-confirmed":
                evidence = MatchingFacts(bundle.ContextFacts, OrderingChannelKeys);
                return evidence.Count > 0;

            case "hours-evidence-present":
            {
                var facts = MatchingFacts(AllFacts(bundle), HoursKeys).ToList();
                if (!string.IsNullOrWhiteSpace(profile.BusinessHours))
                    facts.Add(ProfileEvidence("businessHours", profile.BusinessHours.Trim(), profile.Source));
                evidence = facts;
                return evidence.Count > 0;
            }

            case "current-offer-confirmed":
                evidence = MatchingFacts(bundle.ContextFacts, OfferKeys);
                return evidence.Count > 0;

            case "reputation-signal-present":
                evidence = MatchingFacts(bundle.ContextFacts.Concat(bundle.MemoryFacts), ReputationKeys);
                return evidence.Count > 0;

            default:
                evidence = [];
                return false;
        }
    }

    private static IEnumerable<ResolvedKnowledgeFact> AllFacts(ResolvedKnowledgeBundle bundle) =>
        bundle.ContextFacts.Concat(bundle.LocalMarketFacts).Concat(bundle.MemoryFacts).Concat(bundle.OperationalFacts);

    private static IReadOnlyList<OpportunityEvidenceReference> MatchingFacts(
        IEnumerable<ResolvedKnowledgeFact> facts,
        IReadOnlySet<string> keys) =>
        facts.Where(x => keys.Contains(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
            .OrderBy(x => x.Layer, StringComparer.Ordinal)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Value, StringComparer.Ordinal)
            .Select(FactEvidence)
            .ToArray();

    private static OpportunityEvidenceReference FactEvidence(ResolvedKnowledgeFact fact) =>
        new(EvidenceId(fact.Layer, fact.Key, fact.Value, fact.Source), fact.Layer, fact.Key, fact.Value, fact.Source);

    private static OpportunityEvidenceReference ProfileEvidence(string key, string value, string source) =>
        new(EvidenceId("profile", key, value, source), "profile", key, value, source);

    private static OpportunityEvidenceReference PolicyEvidence(string key, string value, string source) =>
        new(EvidenceId("policy", key, value, source), "policy", key, value, source);

    private static string EvidenceId(string layer, string key, string value, string source)
    {
        var canonical = string.Join('\u001f', layer.Trim(), key.Trim(), value.Trim(), source.Trim());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static GeneratedOpportunityCandidate CreateCandidate(
        KnowledgePackManifestV2 manifest,
        KnowledgeOpportunityPattern pattern,
        BusinessGoal goal,
        ResolvedKnowledgeBundle bundle,
        IReadOnlyList<OpportunityEvidenceReference> evidence,
        string cooldownFingerprint,
        DateTimeOffset now)
    {
        var confidence = ResolveConfidence(pattern.Confidence, evidence);
        var title = pattern.TitleTemplate.Replace("{goal}", goal.Title.Trim(), StringComparison.Ordinal);
        var categorySpecific = manifest.Layer != KnowledgePackLayers.Core;
        var limitations = new List<string>
        {
            "Expected impact is directional and not guaranteed.",
            "Atlas has not measured an outcome for this action yet.",
            "External action remains owner-controlled and requires owner review."
        };
        if (evidence.Any(IsOperationalEvidence))
            limitations.Add("The observed movement does not prove what caused it; review assumptions before acting.");

        return new GeneratedOpportunityCandidate(
            pattern.Key,
            title,
            goal.Id,
            goal.Type,
            goal.Title,
            goal.Priority,
            $"Aligned to priority #{goal.Priority}: {goal.Title}",
            pattern.WhyItMattersTemplate,
            pattern.WhyNowTemplate,
            pattern.ExpectedImpact,
            pattern.Effort,
            confidence,
            evidence,
            [
                "The owner-confirmed Business Profile and selected goal remain current.",
                "The supplied evidence remains accurate enough for this bounded review."
            ],
            limitations,
            pattern.ExecutionTemplateKey,
            pattern.CooldownDays,
            manifest.PackKey,
            manifest.ExactVersion,
            bundle.Manifests.ToArray(),
            bundle.Fingerprint,
            cooldownFingerprint,
            now,
            now.AddDays(1),
            categorySpecific);
    }

    private static string ResolveConfidence(string manifestConfidence, IReadOnlyList<OpportunityEvidenceReference> evidence)
    {
        if (string.Equals(manifestConfidence, "Low", StringComparison.OrdinalIgnoreCase)) return "Low";
        if (evidence.Where(IsOperationalEvidence).Any(item =>
                OperationalChangeEvidenceCodec.TryParse(
                    new(item.Layer, item.Key, item.Value, item.Source), out var parsed) &&
                parsed?.Freshness == OperationalFreshness.Stale))
            return "Low";

        var hasNonOwnerEvidence = evidence.Any(x =>
            x.Layer != "policy" &&
            x.Layer != KnowledgeEvidenceLayers.Operational &&
            !string.Equals(x.Source, FieldSources.Owner, StringComparison.OrdinalIgnoreCase));
        return hasNonOwnerEvidence ? "Low" : "Medium";
    }

    private static int OperationalPriorityRank(GeneratedOpportunityCandidate candidate)
    {
        var operational = candidate.Evidence
            .Where(IsOperationalEvidence)
            .Select(TryParseOperationalEvidence)
            .Where(x => x is not null)
            .ToArray();
        if (operational.Any(x => x!.Freshness == OperationalFreshness.Fresh)) return 0;
        return operational.Length == 0 ? 1 : 2;
    }

    private static decimal OperationalMateriality(GeneratedOpportunityCandidate candidate) =>
        candidate.Evidence
            .Where(IsOperationalEvidence)
            .Select(TryParseOperationalEvidence)
            .Where(x => x?.Freshness == OperationalFreshness.Fresh)
            .Select(x => Math.Abs(x!.RelativeDelta))
            .DefaultIfEmpty(0m)
            .Max();

    private static OperationalChangeEvidence? TryParseOperationalEvidence(OpportunityEvidenceReference evidence)
    {
        OperationalChangeEvidenceCodec.TryParse(
            new(evidence.Layer, evidence.Key, evidence.Value, evidence.Source), out var parsed);
        return parsed;
    }

    private static bool IsOperationalEvidence(OpportunityEvidenceReference evidence) =>
        evidence.Layer == KnowledgeEvidenceLayers.Operational &&
        OperationalChangeEvidenceCodec.TryParse(
            new(evidence.Layer, evidence.Key, evidence.Value, evidence.Source), out _);

    private static bool IsSuppressed(
        string patternKey,
        int cooldownDays,
        string cooldownFingerprint,
        IReadOnlyCollection<Opportunity> priorOpportunities,
        DateTimeOffset now)
    {
        if (cooldownDays <= 0) return false;
        var threshold = now.AddDays(-cooldownDays);
        foreach (var prior in priorOpportunities)
        {
            if (prior.CreatedAt < threshold || prior.CreatedAt > now) continue;
            if (!OpportunityGenerationSnapshot.TryReadCooldownIdentity(prior.EvidenceJson, out var priorPattern, out var priorFingerprint) ||
                !string.Equals(priorPattern, patternKey, StringComparison.Ordinal))
                continue;

            if (priorFingerprint is null || string.Equals(priorFingerprint, cooldownFingerprint, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static int ConfidenceRank(string value) => value switch
    {
        "Medium" => 0,
        "Low" => 1,
        _ => 2
    };

    private static int EffortRank(string value) => value switch
    {
        "Low" => 0,
        "Medium" => 1,
        "High" => 2,
        _ => 3
    };
}

public static class OpportunityGenerationSnapshot
{
    public const int SchemaVersion = 2;

    public static string Serialize(GeneratedOpportunityCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return JsonSerializer.Serialize(new
        {
            schemaVersion = SchemaVersion,
            patternKey = candidate.PatternKey,
            bundleFingerprint = candidate.BundleFingerprint,
            cooldownFingerprint = candidate.CooldownFingerprint,
            goal = new
            {
                id = candidate.GoalId,
                type = candidate.GoalType,
                title = candidate.GoalTitle,
                priority = candidate.GoalPriority,
                alignment = candidate.GoalAlignment
            },
            manifests = candidate.Manifests.Select(x => new
            {
                layer = x.Layer,
                packKey = x.PackKey,
                exactVersion = x.ExactVersion,
                fingerprint = x.Fingerprint
            }).ToArray(),
            evidence = candidate.Evidence.Select(x => new
            {
                evidenceId = x.EvidenceId,
                layer = x.Layer,
                key = x.Key,
                value = x.Value,
                source = x.Source
            }).ToArray(),
            assumptions = candidate.Assumptions,
            limitations = candidate.Limitations,
            executionTemplateKey = candidate.ExecutionTemplateKey,
            cooldownDays = candidate.CooldownDays,
            generatedAt = candidate.GeneratedAt
        });
    }

    public static string ComputeCooldownFingerprint(
        string patternKey,
        Guid goalId,
        string goalType,
        string goalTitle,
        int goalPriority,
        IEnumerable<string> evidenceIds)
    {
        ArgumentNullException.ThrowIfNull(evidenceIds);
        var parts = new List<string>
        {
            patternKey.Trim(),
            goalId.ToString("D"),
            goalType.Trim().ToLowerInvariant(),
            goalTitle.Trim(),
            goalPriority.ToString(CultureInfo.InvariantCulture)
        };
        parts.AddRange(evidenceIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal));
        var canonical = string.Join('\u001f', parts);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static bool TryReadCooldownIdentity(string? json, out string? patternKey, out string? cooldownFingerprint)
    {
        patternKey = null;
        cooldownFingerprint = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("patternKey", out var pattern) || pattern.ValueKind != JsonValueKind.String)
                return false;
            patternKey = pattern.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(patternKey)) return false;

            if (root.TryGetProperty("cooldownFingerprint", out var fingerprint) &&
                fingerprint.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(fingerprint.GetString()))
            {
                cooldownFingerprint = fingerprint.GetString()!.Trim();
                return true;
            }

            if (!root.TryGetProperty("goal", out var goal) || goal.ValueKind != JsonValueKind.Object)
                return true;
            if (!goal.TryGetProperty("id", out var goalIdElement) ||
                goalIdElement.ValueKind != JsonValueKind.String ||
                !Guid.TryParse(goalIdElement.GetString(), out var goalId))
                return true;
            if (!goal.TryGetProperty("type", out var goalTypeElement) ||
                goalTypeElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(goalTypeElement.GetString()))
                return true;
            if (!goal.TryGetProperty("title", out var goalTitleElement) ||
                goalTitleElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(goalTitleElement.GetString()))
                return true;
            if (!goal.TryGetProperty("priority", out var goalPriorityElement) ||
                goalPriorityElement.ValueKind != JsonValueKind.Number ||
                !goalPriorityElement.TryGetInt32(out var goalPriority))
                return true;
            if (!root.TryGetProperty("evidence", out var evidence) || evidence.ValueKind != JsonValueKind.Array)
                return true;

            var evidenceIds = new List<string>();
            foreach (var item in evidence.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("evidenceId", out var evidenceIdElement) ||
                    evidenceIdElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(evidenceIdElement.GetString()))
                    return true;
                evidenceIds.Add(evidenceIdElement.GetString()!.Trim());
            }

            cooldownFingerprint = ComputeCooldownFingerprint(
                patternKey,
                goalId,
                goalTypeElement.GetString()!,
                goalTitleElement.GetString()!,
                goalPriority,
                evidenceIds);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryReadPatternKey(string? json, out string? patternKey) =>
        TryReadCooldownIdentity(json, out patternKey, out _);
}
