using System.Text.Json;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class KnowledgePackManifestV2Tests
{
    [Fact]
    public void Core_manifest_is_valid_and_industry_agnostic()
    {
        var manifest = GenericBusinessKnowledgeManifestV2.Create();

        Assert.Empty(KnowledgePackManifestV2Policy.Validate(manifest));
        Assert.Equal(2, manifest.SchemaVersion);
        Assert.Equal(KnowledgePackLayers.Core, manifest.Layer);
        Assert.Empty(manifest.SupportedCategoryKeys);
        Assert.DoesNotContain("restaurant", JsonSerializer.Serialize(manifest), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fingerprint_is_stable_when_unordered_sections_are_reordered()
    {
        var manifest = GenericBusinessKnowledgeManifestV2.Create();
        var reordered = manifest with
        {
            Kpis = manifest.Kpis.Reverse().ToArray(),
            EvidenceRules = manifest.EvidenceRules.Reverse().ToArray(),
            OpportunityPatterns = manifest.OpportunityPatterns.Reverse().ToArray(),
            ExecutionTemplates = manifest.ExecutionTemplates.Reverse().ToArray(),
            MeasurementSuggestions = manifest.MeasurementSuggestions.Reverse().ToArray(),
            Seasonality = manifest.Seasonality.Reverse().ToArray(),
            Guardrails = manifest.Guardrails.Reverse().ToArray()
        };

        Assert.Equal(
            KnowledgePackManifestV2Policy.Fingerprint(manifest),
            KnowledgePackManifestV2Policy.Fingerprint(reordered));
    }

    [Fact]
    public void Duplicate_stable_keys_are_rejected()
    {
        var manifest = GenericBusinessKnowledgeManifestV2.Create();
        var duplicated = manifest with { Kpis = [manifest.Kpis[0], manifest.Kpis[0]] };

        var errors = KnowledgePackManifestV2Policy.Validate(duplicated);

        Assert.Contains("kpis", errors.Keys);
    }

    [Fact]
    public void Broken_pattern_references_are_rejected()
    {
        var manifest = GenericBusinessKnowledgeManifestV2.Create();
        var broken = manifest with
        {
            OpportunityPatterns =
            [
                new KnowledgeOpportunityPattern(
                    "broken-pattern",
                    "Review a practical action",
                    ["revenue"],
                    ["missing-evidence-rule"],
                    "It supports a confirmed goal.",
                    "The current context is sufficient to review it now.",
                    "Directional improvement only; no result is guaranteed.",
                    "Low",
                    "Medium",
                    "missing-template",
                    7)
            ]
        };

        var errors = KnowledgePackManifestV2Policy.Validate(broken);

        Assert.Contains("opportunityPatterns", errors.Keys);
    }

    [Fact]
    public void Category_layer_requires_known_canonical_categories()
    {
        var manifest = GenericBusinessKnowledgeManifestV2.Create() with
        {
            PackKey = "category-unknown",
            Layer = KnowledgePackLayers.Category,
            SupportedCategoryKeys = ["not-a-real-category"]
        };

        var errors = KnowledgePackManifestV2Policy.Validate(manifest);

        Assert.Contains("supportedCategoryKeys", errors.Keys);
    }

    [Fact]
    public void Valid_operational_evidence_requirement_is_accepted()
    {
        var manifest = WithOperationalRequirement(new(
            "gross-sales", OperationalChangeDirections.Decrease, .10m, [7, 28],
            [OperationalFreshness.Fresh, OperationalFreshness.Stale]));

        Assert.Empty(KnowledgePackManifestV2Policy.Validate(manifest));
    }

    [Theory]
    [MemberData(nameof(InvalidOperationalRequirements))]
    public void Invalid_operational_evidence_requirements_are_rejected(KnowledgeOperationalEvidenceRequirement requirement)
    {
        var errors = KnowledgePackManifestV2Policy.Validate(WithOperationalRequirement(requirement));

        Assert.Contains("evidenceRules", errors.Keys);
    }

    [Fact]
    public void Operational_requirement_fields_are_fingerprinted()
    {
        var original = WithOperationalRequirement(new(
            "gross-sales", OperationalChangeDirections.Decrease, .10m, [7, 28],
            [OperationalFreshness.Fresh, OperationalFreshness.Stale]));
        var changed = WithOperationalRequirement(new(
            "orders", OperationalChangeDirections.Increase, .20m, [28],
            [OperationalFreshness.Stale]));

        Assert.NotEqual(
            KnowledgePackManifestV2Policy.Fingerprint(original),
            KnowledgePackManifestV2Policy.Fingerprint(changed));
    }

    public static TheoryData<KnowledgeOperationalEvidenceRequirement> InvalidOperationalRequirements => new()
    {
        new("", OperationalChangeDirections.Decrease, .10m, [7], [OperationalFreshness.Fresh]),
        new("Gross Sales", OperationalChangeDirections.Decrease, .10m, [7], [OperationalFreshness.Fresh]),
        new("gross-sales", "sideways", .10m, [7], [OperationalFreshness.Fresh]),
        new("gross-sales", OperationalChangeDirections.Decrease, 0m, [7], [OperationalFreshness.Fresh]),
        new("gross-sales", OperationalChangeDirections.Decrease, 1.01m, [7], [OperationalFreshness.Fresh]),
        new("gross-sales", OperationalChangeDirections.Decrease, .10m, [14], [OperationalFreshness.Fresh]),
        new("gross-sales", OperationalChangeDirections.Decrease, .10m, [7, 7], [OperationalFreshness.Fresh]),
        new("gross-sales", OperationalChangeDirections.Decrease, .10m, [7], [OperationalFreshness.Historical]),
        new("gross-sales", OperationalChangeDirections.Decrease, .10m, [7], [OperationalFreshness.Fresh, OperationalFreshness.Fresh])
    };

    private static KnowledgePackManifestV2 WithOperationalRequirement(KnowledgeOperationalEvidenceRequirement requirement)
    {
        var manifest = GenericBusinessKnowledgeManifestV2.Create();
        return manifest with
        {
            EvidenceRules =
            [
                new KnowledgeEvidenceRule(
                    "sales-decline-observed",
                    "Require a material observed sales decline.",
                    1,
                    false)
                {
                    OperationalRequirement = requirement
                }
            ],
            OpportunityPatterns = []
        };
    }
}
