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
        Assert.Equal("1.1", manifest.ExactVersion);
        Assert.Equal(KnowledgePackLayers.Category, manifest.Layer);
        Assert.Equal(["restaurant-cafe"], manifest.SupportedCategoryKeys);
        Assert.Equal(["restaurant", "cafe", "bakery", "takeaway"], manifest.SupportedSubcategoryKeys);
        Assert.True(manifest.OpportunityPatterns.Count >= 4);
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

    [Fact]
    public void Operational_rules_encode_the_exact_pilot_policy()
    {
        var rules = RestaurantCafeKnowledgeManifestV2.Create().EvidenceRules
            .Where(rule => rule.OperationalRequirement is not null)
            .ToDictionary(rule => rule.OperationalRequirement!.MetricKey, StringComparer.Ordinal);

        Assert.Equal(4, rules.Count);
        AssertRequirement(rules["gross-sales"], OperationalChangeDirections.Decrease);
        AssertRequirement(rules["orders"], OperationalChangeDirections.Decrease);
        AssertRequirement(rules["repeat-orders"], OperationalChangeDirections.Decrease);
        AssertRequirement(rules["delivery-time"], OperationalChangeDirections.Increase);
    }

    [Fact]
    public void Operational_patterns_have_approved_goal_mappings_and_safe_owner_controlled_copy()
    {
        var manifest = RestaurantCafeKnowledgeManifestV2.Create();
        var patterns = manifest.OpportunityPatterns
            .Where(pattern => OperationalPatternKeys.Contains(pattern.Key))
            .ToDictionary(pattern => pattern.Key, StringComparer.Ordinal);

        Assert.Equal(OperationalPatternKeys.Order(), patterns.Keys.Order());
        Assert.Equal(["revenue", "growth"], patterns["sales-decline-review"].GoalTypes);
        Assert.Equal(["revenue", "growth"], patterns["order-decline-review"].GoalTypes);
        Assert.Equal(["retention", "customer-experience"], patterns["repeat-order-decline-review"].GoalTypes);
        Assert.Equal(["efficiency", "customer-experience"], patterns["delivery-time-deterioration-review"].GoalTypes);

        var templateKeys = patterns.Values.Select(pattern => pattern.ExecutionTemplateKey).ToHashSet(StringComparer.Ordinal);
        var templates = manifest.ExecutionTemplates.Where(template => templateKeys.Contains(template.Key)).ToArray();
        Assert.Equal(4, templates.Length);
        Assert.All(templates, template => Assert.Contains("review", template.ContentTemplate, StringComparison.OrdinalIgnoreCase));
        Assert.All(templates, template => Assert.True(
            template.ContentTemplate.Contains("experiment", StringComparison.OrdinalIgnoreCase) ||
            template.ContentTemplate.Contains("checklist", StringComparison.OrdinalIgnoreCase)));

        var operationalCopy = JsonSerializer.Serialize(new
        {
            Patterns = patterns.Values,
            Templates = templates
        });
        foreach (var prohibited in new[] { "caused", "guarantee", "blame", "customer segment", "provider", "employee", "staff", "courier", "menu item" })
            Assert.DoesNotContain(prohibited, operationalCopy, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly string[] OperationalPatternKeys =
    [
        "sales-decline-review",
        "order-decline-review",
        "repeat-order-decline-review",
        "delivery-time-deterioration-review"
    ];

    private static void AssertRequirement(KnowledgeEvidenceRule rule, string direction)
    {
        var requirement = Assert.IsType<KnowledgeOperationalEvidenceRequirement>(rule.OperationalRequirement);
        Assert.Equal(direction, requirement.Direction);
        Assert.Equal(.10m, requirement.MinimumRelativeChange);
        Assert.Equal([7, 28], requirement.Windows);
        Assert.Equal([OperationalFreshness.Fresh, OperationalFreshness.Stale], requirement.Freshness);
        Assert.Equal(1, rule.MinimumEvidenceCount);
    }
}
