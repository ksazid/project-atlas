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
}
