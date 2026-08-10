using System.Text.Json;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class RestaurantCafeKnowledgeManifestV2Tests
{
    [Fact]
    public void Reference_manifest_is_valid_and_targets_only_restaurant_cafe()
    {
        var manifest = RestaurantCafeKnowledgeManifestV2.Create();

        Assert.Empty(KnowledgePackManifestV2Policy.Validate(manifest));
        Assert.Equal(2, manifest.SchemaVersion);
        Assert.Equal("restaurant-cafe-intelligence", manifest.PackKey);
        Assert.Equal("1.0", manifest.ExactVersion);
        Assert.Equal(KnowledgePackLayers.Category, manifest.Layer);
        Assert.Equal(["restaurant-cafe"], manifest.SupportedCategoryKeys);
        Assert.Equal(["restaurant", "cafe", "bakery", "takeaway"], manifest.SupportedSubcategoryKeys);
        Assert.Equal(4, manifest.OpportunityPatterns.Count);
    }

    [Fact]
    public void Reference_manifest_is_provider_neutral_and_contains_no_private_api_dependency()
    {
        var json = JsonSerializer.Serialize(RestaurantCafeKnowledgeManifestV2.Create());

        Assert.DoesNotContain("wolt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bolt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api key", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("guarantee", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Every_pattern_has_declared_evidence_and_execution_references()
    {
        var manifest = RestaurantCafeKnowledgeManifestV2.Create();
        var evidence = manifest.EvidenceRules.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        var templates = manifest.ExecutionTemplates.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);

        Assert.All(manifest.OpportunityPatterns, pattern =>
        {
            Assert.NotEmpty(pattern.EvidenceRuleKeys);
            Assert.All(pattern.EvidenceRuleKeys, key => Assert.Contains(key, evidence));
            Assert.Contains(pattern.ExecutionTemplateKey, templates);
        });
    }
}
