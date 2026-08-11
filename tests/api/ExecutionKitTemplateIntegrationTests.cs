using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class ExecutionKitTemplateIntegrationTests
{
    [Fact]
    public void Restaurant_opportunity_uses_exact_manifest_execution_checklist()
    {
        var now = new DateTimeOffset(2026, 8, 11, 3, 0, 0, TimeSpan.Zero);
        var opportunity = RestaurantOrderingOpportunity(now);
        var manifest = RestaurantCafeKnowledgeManifestV2.Create();
        var expected = manifest.ExecutionTemplates.Single(x => x.Key == "ordering-path-review-checklist");

        var kit = ExecutionKitFactory.Create(opportunity, now);

        Assert.Equal(RestaurantCafeKnowledgeManifestV2.PackKey, kit.KnowledgePackKey);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.Version, kit.KnowledgePackVersion);
        Assert.Contains(kit.Assets, asset =>
            asset.Type == expected.AssetType &&
            asset.Title == expected.Title &&
            asset.Content == expected.ContentTemplate);
        Assert.DoesNotContain(kit.Assets, asset => asset.Title == "Action checklist");
    }

    [Fact]
    public void Core_opportunity_uses_exact_core_execution_checklist()
    {
        var now = new DateTimeOffset(2026, 8, 11, 3, 0, 0, TimeSpan.Zero);
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var businessId = Guid.NewGuid();
        var profile = new BusinessProfile
        {
            BusinessId = businessId, Language = "en", Source = FieldSources.Owner,
            OwnerConfirmed = true, UpdatedAt = now
        };
        var goal = new BusinessGoal
        {
            Id = Guid.NewGuid(), BusinessId = businessId, Type = "growth", Title = "Grow the business",
            Priority = 1, UpdatedAt = now
        };
        var bundle = new ResolvedKnowledgeBundle(
            "retail", null,
            [new ResolvedKnowledgeManifest(core.Layer, core.PackKey, core.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(core))],
            [], [], [], "core-execution-bundle");
        var candidate = OpportunityGenerator.Generate(profile, [goal], bundle, [], now).Selected!;
        var opportunity = OpportunityFrom(candidate, businessId, now);
        var expected = core.ExecutionTemplates.Single(x => x.Key == "practical-action-checklist");

        var kit = ExecutionKitFactory.Create(opportunity, now);

        Assert.Contains(kit.Assets, asset =>
            asset.Type == expected.AssetType &&
            asset.Title == expected.Title &&
            asset.Content == expected.ContentTemplate);
    }

    [Fact]
    public void Pack_version_mismatch_never_leaks_category_template_and_falls_back_safely()
    {
        var now = new DateTimeOffset(2026, 8, 11, 3, 0, 0, TimeSpan.Zero);
        var opportunity = RestaurantOrderingOpportunity(now);
        opportunity.KnowledgePackVersion = "999.0";

        var kit = ExecutionKitFactory.Create(opportunity, now);

        Assert.Contains(kit.Assets, asset => asset.Type == ExecutionAssetTypes.Checklist && asset.Title == "Action checklist");
        Assert.DoesNotContain(kit.Assets, asset => asset.Title == "Ordering path review");
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("{\"schemaVersion\":1,\"executionTemplateKey\":\"unknown-template\"}")]
    public void Malformed_or_unknown_snapshot_falls_back_without_throwing(string evidenceJson)
    {
        var now = new DateTimeOffset(2026, 8, 11, 3, 0, 0, TimeSpan.Zero);
        var opportunity = RestaurantOrderingOpportunity(now);
        opportunity.EvidenceJson = evidenceJson;

        var kit = ExecutionKitFactory.Create(opportunity, now);

        Assert.Contains(kit.Assets, asset => asset.Type == ExecutionAssetTypes.Checklist && asset.Title == "Action checklist");
        Assert.Equal("ready", kit.Status);
    }

    private static Opportunity RestaurantOrderingOpportunity(DateTimeOffset now)
    {
        var businessId = Guid.NewGuid();
        var profile = new BusinessProfile
        {
            BusinessId = businessId, Language = "en", Source = FieldSources.Owner,
            OwnerConfirmed = true, UpdatedAt = now
        };
        var goal = new BusinessGoal
        {
            Id = Guid.NewGuid(), BusinessId = businessId, Type = "revenue", Title = "Increase revenue",
            Priority = 1, UpdatedAt = now
        };
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var restaurant = RestaurantCafeKnowledgeManifestV2.Create();
        var bundle = new ResolvedKnowledgeBundle(
            "restaurant-cafe", null,
            [
                new ResolvedKnowledgeManifest(core.Layer, core.PackKey, core.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(core)),
                new ResolvedKnowledgeManifest(restaurant.Layer, restaurant.PackKey, restaurant.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(restaurant))
            ],
            [new ResolvedKnowledgeFact("context", "primarychannels", "Takeaway", FieldSources.Owner)],
            [], [], "restaurant-execution-bundle");
        var candidate = OpportunityGenerator.Generate(profile, [goal], bundle, [], now).Selected!;
        return OpportunityFrom(candidate, businessId, now);
    }

    private static Opportunity OpportunityFrom(GeneratedOpportunityCandidate candidate, Guid businessId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, GoalId = candidate.GoalId,
        Title = candidate.Title, WhyItMatters = candidate.Reason, WhyNow = candidate.WhyNow,
        ExpectedImpact = candidate.ExpectedImpact, Effort = candidate.Effort, Confidence = candidate.Confidence,
        EvidenceSummary = "Structured evidence", EvidenceJson = OpportunityGenerationSnapshot.Serialize(candidate),
        Status = OpportunityStatuses.Available, KnowledgePackKey = candidate.KnowledgePackKey,
        KnowledgePackVersion = candidate.KnowledgePackVersion, KnowledgePackVersionId = Guid.NewGuid(),
        CreatedAt = now, ExpiresAt = candidate.ExpiresAt
    };
}
