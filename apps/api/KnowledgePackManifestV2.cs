using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Atlas.Api;

public static class KnowledgePackLayers
{
    public const string Core = "core";
    public const string Category = "category";
    public const string Subcategory = "subcategory";
    public const string LocalMarket = "local-market";

    public static bool IsValid(string? value) => value is Core or Category or Subcategory or LocalMarket;
}

public sealed record KnowledgeKpiDefinition(string Key, string Label, string Description);

public sealed record KnowledgeEvidenceRule(
    string Key,
    string Description,
    int MinimumEvidenceCount,
    bool OwnerConfirmationRequired);

public sealed record KnowledgeOpportunityPattern(
    string Key,
    string TitleTemplate,
    IReadOnlyList<string> GoalTypes,
    IReadOnlyList<string> EvidenceRuleKeys,
    string WhyItMattersTemplate,
    string WhyNowTemplate,
    string ExpectedImpact,
    string Effort,
    string Confidence,
    string ExecutionTemplateKey,
    int CooldownDays);

public sealed record KnowledgeExecutionTemplate(
    string Key,
    string AssetType,
    string Title,
    string ContentTemplate);

public sealed record KnowledgePackManifestV2(
    int SchemaVersion,
    string PackKey,
    string ExactVersion,
    string Layer,
    IReadOnlyList<string> SupportedCategoryKeys,
    IReadOnlyList<string> SupportedSubcategoryKeys,
    IReadOnlyList<KnowledgeKpiDefinition> Kpis,
    IReadOnlyList<KnowledgeEvidenceRule> EvidenceRules,
    IReadOnlyList<KnowledgeOpportunityPattern> OpportunityPatterns,
    IReadOnlyList<KnowledgeExecutionTemplate> ExecutionTemplates,
    IReadOnlyList<string> MeasurementSuggestions,
    IReadOnlyList<string> Seasonality,
    IReadOnlyList<string> Guardrails);

public static class KnowledgePackManifestV2Policy
{
    private const int MaxSectionItems = 64;
    private const int MaxTextLength = 4000;

    public static IReadOnlyDictionary<string, string[]> Validate(KnowledgePackManifestV2 manifest)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        if (manifest.SchemaVersion != 2) Add(errors, "schemaVersion", "Schema version must be 2.");
        if (!KnowledgePackKeys.IsValid(manifest.PackKey)) Add(errors, "packKey", "Pack key is invalid.");
        if (string.IsNullOrWhiteSpace(manifest.ExactVersion) || manifest.ExactVersion.Length > 40)
            Add(errors, "exactVersion", "Exact version is required and must be at most 40 characters.");
        if (!KnowledgePackLayers.IsValid(manifest.Layer)) Add(errors, "layer", "Knowledge Pack layer is invalid.");

        ValidateCount(errors, "supportedCategoryKeys", manifest.SupportedCategoryKeys.Count);
        ValidateCount(errors, "supportedSubcategoryKeys", manifest.SupportedSubcategoryKeys.Count);
        ValidateCount(errors, "kpis", manifest.Kpis.Count);
        ValidateCount(errors, "evidenceRules", manifest.EvidenceRules.Count);
        ValidateCount(errors, "opportunityPatterns", manifest.OpportunityPatterns.Count);
        ValidateCount(errors, "executionTemplates", manifest.ExecutionTemplates.Count);
        ValidateCount(errors, "measurementSuggestions", manifest.MeasurementSuggestions.Count);
        ValidateCount(errors, "seasonality", manifest.Seasonality.Count);
        ValidateCount(errors, "guardrails", manifest.Guardrails.Count);

        if (manifest.Layer == KnowledgePackLayers.Core &&
            (manifest.SupportedCategoryKeys.Count > 0 || manifest.SupportedSubcategoryKeys.Count > 0))
            Add(errors, "supportedCategoryKeys", "Core manifests cannot claim category or subcategory specificity.");

        if (manifest.Layer is KnowledgePackLayers.Category or KnowledgePackLayers.Subcategory &&
            manifest.SupportedCategoryKeys.Count == 0)
            Add(errors, "supportedCategoryKeys", "Category and subcategory manifests require at least one canonical category.");

        if (manifest.Layer == KnowledgePackLayers.Subcategory && manifest.SupportedSubcategoryKeys.Count == 0)
            Add(errors, "supportedSubcategoryKeys", "Subcategory manifests require at least one canonical subcategory.");

        if (HasDuplicate(manifest.SupportedCategoryKeys))
            Add(errors, "supportedCategoryKeys", "Supported category keys must be unique.");
        if (HasDuplicate(manifest.SupportedSubcategoryKeys))
            Add(errors, "supportedSubcategoryKeys", "Supported subcategory keys must be unique.");

        foreach (var category in manifest.SupportedCategoryKeys)
        {
            if (!BusinessCategoryTaxonomy.IsKnownCategory(category))
                Add(errors, "supportedCategoryKeys", $"Unknown canonical category '{category}'.");
        }

        foreach (var subcategory in manifest.SupportedSubcategoryKeys)
        {
            var known = manifest.SupportedCategoryKeys.Any(category =>
                BusinessCategoryTaxonomy.IsKnownSubcategory(category, subcategory));
            if (!known)
                Add(errors, "supportedSubcategoryKeys", $"Unknown canonical subcategory '{subcategory}' for the supported categories.");
        }

        ValidateStableKeys(errors, "kpis", manifest.Kpis.Select(item => item.Key));
        ValidateStableKeys(errors, "evidenceRules", manifest.EvidenceRules.Select(item => item.Key));
        ValidateStableKeys(errors, "opportunityPatterns", manifest.OpportunityPatterns.Select(item => item.Key));
        ValidateStableKeys(errors, "executionTemplates", manifest.ExecutionTemplates.Select(item => item.Key));

        foreach (var kpi in manifest.Kpis)
        {
            ValidateText(errors, "kpis", kpi.Label);
            ValidateText(errors, "kpis", kpi.Description);
        }

        foreach (var evidence in manifest.EvidenceRules)
        {
            ValidateText(errors, "evidenceRules", evidence.Description);
            if (evidence.MinimumEvidenceCount is < 1 or > 20)
                Add(errors, "evidenceRules", $"Evidence rule '{evidence.Key}' must require between 1 and 20 evidence items.");
        }

        var evidenceKeys = manifest.EvidenceRules.Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        var executionTemplateKeys = manifest.ExecutionTemplates.Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var pattern in manifest.OpportunityPatterns)
        {
            ValidateText(errors, "opportunityPatterns", pattern.TitleTemplate);
            ValidateText(errors, "opportunityPatterns", pattern.WhyItMattersTemplate);
            ValidateText(errors, "opportunityPatterns", pattern.WhyNowTemplate);
            ValidateText(errors, "opportunityPatterns", pattern.ExpectedImpact);
            ValidateText(errors, "opportunityPatterns", pattern.Effort);
            ValidateText(errors, "opportunityPatterns", pattern.Confidence);
            if (pattern.GoalTypes.Count == 0 || HasDuplicate(pattern.GoalTypes))
                Add(errors, "opportunityPatterns", $"Opportunity pattern '{pattern.Key}' requires unique goal types.");
            if (pattern.EvidenceRuleKeys.Count == 0 || HasDuplicate(pattern.EvidenceRuleKeys))
                Add(errors, "opportunityPatterns", $"Opportunity pattern '{pattern.Key}' requires unique evidence-rule references.");
            if (pattern.EvidenceRuleKeys.Any(key => !evidenceKeys.Contains(key)))
                Add(errors, "opportunityPatterns", $"Opportunity pattern '{pattern.Key}' references an unknown evidence rule.");
            if (!executionTemplateKeys.Contains(pattern.ExecutionTemplateKey))
                Add(errors, "opportunityPatterns", $"Opportunity pattern '{pattern.Key}' references an unknown execution template.");
            if (pattern.CooldownDays is < 0 or > 365)
                Add(errors, "opportunityPatterns", $"Opportunity pattern '{pattern.Key}' cooldown must be between 0 and 365 days.");
        }

        foreach (var template in manifest.ExecutionTemplates)
        {
            ValidateText(errors, "executionTemplates", template.AssetType);
            ValidateText(errors, "executionTemplates", template.Title);
            ValidateText(errors, "executionTemplates", template.ContentTemplate);
        }

        ValidateTextCollection(errors, "measurementSuggestions", manifest.MeasurementSuggestions);
        ValidateTextCollection(errors, "seasonality", manifest.Seasonality);
        ValidateTextCollection(errors, "guardrails", manifest.Guardrails);
        if (manifest.Guardrails.Count == 0) Add(errors, "guardrails", "At least one guardrail is required.");

        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
    }

    public static string Fingerprint(KnowledgePackManifestV2 manifest)
    {
        var canonical = new
        {
            manifest.SchemaVersion,
            manifest.PackKey,
            manifest.ExactVersion,
            manifest.Layer,
            SupportedCategoryKeys = manifest.SupportedCategoryKeys.Order(StringComparer.Ordinal).ToArray(),
            SupportedSubcategoryKeys = manifest.SupportedSubcategoryKeys.Order(StringComparer.Ordinal).ToArray(),
            Kpis = manifest.Kpis.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => new { item.Key, item.Label, item.Description }).ToArray(),
            EvidenceRules = manifest.EvidenceRules.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => new { item.Key, item.Description, item.MinimumEvidenceCount, item.OwnerConfirmationRequired }).ToArray(),
            OpportunityPatterns = manifest.OpportunityPatterns.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => new
                {
                    item.Key,
                    item.TitleTemplate,
                    GoalTypes = item.GoalTypes.Order(StringComparer.Ordinal).ToArray(),
                    EvidenceRuleKeys = item.EvidenceRuleKeys.Order(StringComparer.Ordinal).ToArray(),
                    item.WhyItMattersTemplate,
                    item.WhyNowTemplate,
                    item.ExpectedImpact,
                    item.Effort,
                    item.Confidence,
                    item.ExecutionTemplateKey,
                    item.CooldownDays
                }).ToArray(),
            ExecutionTemplates = manifest.ExecutionTemplates.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => new { item.Key, item.AssetType, item.Title, item.ContentTemplate }).ToArray(),
            MeasurementSuggestions = manifest.MeasurementSuggestions.Order(StringComparer.Ordinal).ToArray(),
            Seasonality = manifest.Seasonality.Order(StringComparer.Ordinal).ToArray(),
            Guardrails = manifest.Guardrails.Order(StringComparer.Ordinal).ToArray()
        };

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical));
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static void ValidateCount(Dictionary<string, List<string>> errors, string key, int count)
    {
        if (count > MaxSectionItems) Add(errors, key, $"{key} cannot contain more than {MaxSectionItems} items.");
    }

    private static void ValidateStableKeys(Dictionary<string, List<string>> errors, string key, IEnumerable<string> keys)
    {
        var values = keys.ToArray();
        if (values.Any(value => !KnowledgePackKeys.IsValid(value))) Add(errors, key, "Stable keys must use the canonical lower-kebab-case key format.");
        if (HasDuplicate(values)) Add(errors, key, "Stable keys must be unique.");
    }

    private static void ValidateTextCollection(Dictionary<string, List<string>> errors, string key, IEnumerable<string> values)
    {
        foreach (var value in values) ValidateText(errors, key, value);
    }

    private static void ValidateText(Dictionary<string, List<string>> errors, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxTextLength)
            Add(errors, key, $"{key} text must be non-empty and at most {MaxTextLength} characters.");
    }

    private static bool HasDuplicate(IEnumerable<string> values) =>
        values.GroupBy(value => value, StringComparer.Ordinal).Any(group => group.Count() > 1);

    private static void Add(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var bucket))
        {
            bucket = [];
            errors[key] = bucket;
        }
        bucket.Add(message);
    }
}

public static class GenericBusinessKnowledgeManifestV2
{
    public static KnowledgePackManifestV2 Create() => new(
        SchemaVersion: 2,
        PackKey: KnowledgePackKeys.GenericBusiness,
        ExactVersion: GenericBusinessKnowledgePack.InitialVersion,
        Layer: KnowledgePackLayers.Core,
        SupportedCategoryKeys: [],
        SupportedSubcategoryKeys: [],
        Kpis:
        [
            new KnowledgeKpiDefinition("goal-progress", "Goal progress", "Observe progress against the owner-confirmed priority goal without implying causation."),
            new KnowledgeKpiDefinition("action-follow-through", "Action follow-through", "Record whether an agreed practical action was completed and what was observed afterward.")
        ],
        EvidenceRules:
        [
            new KnowledgeEvidenceRule("confirmed-profile", "Use an owner-confirmed Business Profile as the baseline evidence source.", 1, true),
            new KnowledgeEvidenceRule("priority-goal", "Require at least one current owner-confirmed priority goal before proposing an action.", 1, true)
        ],
        OpportunityPatterns:
        [
            new KnowledgeOpportunityPattern(
                "priority-goal-action",
                "Review one practical action for {goal}",
                ["growth", "retention", "efficiency", "customer-experience", "risk-reduction"],
                ["confirmed-profile", "priority-goal"],
                "It supports a current owner-confirmed priority goal.",
                "The confirmed profile and goal provide enough context to review a bounded next step.",
                "A clearer next action and an observable follow-up signal; no business result is guaranteed.",
                "Low",
                "Medium",
                "practical-action-checklist",
                7)
        ],
        ExecutionTemplates:
        [
            new KnowledgeExecutionTemplate(
                "practical-action-checklist",
                "checklist",
                "Practical action checklist",
                "1. Reconfirm the priority goal.\n2. Choose the smallest owner-approved action.\n3. Record one observable baseline.\n4. Complete the action.\n5. Record what changed and what did not.")
        ],
        MeasurementSuggestions:
        [
            "Choose one observable measure before acting and compare it again after the action.",
            "Treat owner-entered observations as owner-reported unless independently verified."
        ],
        Seasonality:
        [
            "Check whether known calendar, demand or operating-cycle effects could change the timing before prioritizing an action."
        ],
        Guardrails:
        [
            "Use confirmed Business Profile, Business Goals and Business Context as the primary evidence base.",
            "Do not invent missing facts, claim guaranteed outcomes or imply causation from an unverified observation.",
            "Keep external actions owner-controlled and preserve the exact Knowledge Pack version used for a recommendation."
        ]);
}
