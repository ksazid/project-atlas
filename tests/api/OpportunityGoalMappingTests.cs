using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OpportunityGoalMappingTests
{
    [Theory]
    [InlineData("revenue", "ordering-path-clarity-review")]
    [InlineData("acquisition", "ordering-path-clarity-review")]
    [InlineData("saved-time", "ordering-path-clarity-review")]
    [InlineData("operational-consistency", "ordering-path-clarity-review")]
    [InlineData("reputation", "ordering-path-clarity-review")]
    public void Existing_Atlas_goal_types_map_to_manifest_intents_without_changing_owner_goal(string goalType, string expectedPattern)
    {
        var businessId = Guid.NewGuid();
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var restaurant = RestaurantCafeKnowledgeManifestV2.Create();
        var bundle = new ResolvedKnowledgeBundle(
            "restaurant-cafe",
            null,
            [
                new ResolvedKnowledgeManifest(core.Layer, core.PackKey, core.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(core)),
                new ResolvedKnowledgeManifest(restaurant.Layer, restaurant.PackKey, restaurant.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(restaurant))
            ],
            [new ResolvedKnowledgeFact("context", "primarychannels", "Takeaway", "owner")],
            [],
            [],
            "goal-map-bundle");
        var profile = new BusinessProfile
        {
            BusinessId = businessId,
            Language = "en",
            Source = FieldSources.Owner,
            OwnerConfirmed = true,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var goal = new BusinessGoal
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Type = goalType,
            Title = "Owner goal",
            Priority = 1,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = OpportunityGenerator.Generate(profile, [goal], bundle, [], DateTimeOffset.UtcNow);

        Assert.Contains(result.Candidates, x => x.PatternKey == expectedPattern && x.GoalType == goalType && x.GoalId == goal.Id);
    }

    [Theory]
    [InlineData("profitability")]
    [InlineData("custom")]
    public void Ambiguous_goal_types_are_not_silently_remapped(string goalType)
    {
        var businessId = Guid.NewGuid();
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var restaurant = RestaurantCafeKnowledgeManifestV2.Create();
        var bundle = new ResolvedKnowledgeBundle(
            "restaurant-cafe",
            null,
            [
                new ResolvedKnowledgeManifest(core.Layer, core.PackKey, core.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(core)),
                new ResolvedKnowledgeManifest(restaurant.Layer, restaurant.PackKey, restaurant.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(restaurant))
            ],
            [new ResolvedKnowledgeFact("context", "primarychannels", "Takeaway", "owner")],
            [],
            [],
            "goal-map-bundle");
        var profile = new BusinessProfile { BusinessId = businessId, Language = "en", Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = DateTimeOffset.UtcNow };
        var goal = new BusinessGoal { Id = Guid.NewGuid(), BusinessId = businessId, Type = goalType, Title = "Owner goal", Priority = 1, UpdatedAt = DateTimeOffset.UtcNow };

        var result = OpportunityGenerator.Generate(profile, [goal], bundle, [], DateTimeOffset.UtcNow);

        Assert.DoesNotContain(result.Candidates, x => x.PatternKey == "ordering-path-clarity-review");
    }
}
