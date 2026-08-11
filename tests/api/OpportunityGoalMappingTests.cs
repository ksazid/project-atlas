using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OpportunityGoalMappingTests
{
    [Theory]
    [InlineData("revenue")]
    [InlineData("acquisition")]
    [InlineData("saved-time")]
    [InlineData("reduced-waste")]
    [InlineData("operational-consistency")]
    public void Existing_Atlas_goal_types_map_to_compatible_manifest_intents_without_changing_owner_goal(string goalType)
    {
        var businessId = Guid.NewGuid();
        var bundle = RestaurantBundle(
            context: [new ResolvedKnowledgeFact("context", "primarychannels", "Takeaway", "owner")]);
        var profile = ConfirmedProfile(businessId);
        var goal = Goal(businessId, goalType);

        var result = OpportunityGenerator.Generate(profile, [goal], bundle, [], DateTimeOffset.UtcNow);

        Assert.Contains(result.Candidates, x =>
            x.PatternKey == "ordering-path-clarity-review" && x.GoalType == goalType && x.GoalId == goal.Id);
    }

    [Fact]
    public void Reputation_goal_maps_only_to_reputation_signal_pattern_when_reputation_evidence_exists()
    {
        var businessId = Guid.NewGuid();
        var bundle = RestaurantBundle(
            context: [new ResolvedKnowledgeFact("context", "primarychannels", "Takeaway", "owner")],
            memory: [new ResolvedKnowledgeFact("memory", "reviewSignal", "Recent reviews mention slow pickup", "public")]);
        var profile = ConfirmedProfile(businessId);
        var goal = Goal(businessId, "reputation");

        var result = OpportunityGenerator.Generate(profile, [goal], bundle, [], DateTimeOffset.UtcNow);

        Assert.Contains(result.Candidates, x => x.PatternKey == "reputation-signal-follow-up" && x.GoalId == goal.Id);
        Assert.DoesNotContain(result.Candidates, x => x.PatternKey == "ordering-path-clarity-review" && x.GoalId == goal.Id);
        Assert.DoesNotContain(result.Candidates, x => x.PatternKey == "priority-goal-action" && x.GoalId == goal.Id);
    }

    [Theory]
    [InlineData("profitability")]
    [InlineData("custom")]
    public void Ambiguous_goal_types_are_not_silently_remapped(string goalType)
    {
        var businessId = Guid.NewGuid();
        var bundle = RestaurantBundle(
            context: [new ResolvedKnowledgeFact("context", "primarychannels", "Takeaway", "owner")]);
        var profile = ConfirmedProfile(businessId);
        var goal = Goal(businessId, goalType);

        var result = OpportunityGenerator.Generate(profile, [goal], bundle, [], DateTimeOffset.UtcNow);

        Assert.Empty(result.Candidates);
    }

    private static BusinessProfile ConfirmedProfile(Guid businessId) => new()
    {
        BusinessId = businessId,
        Language = "en",
        Source = FieldSources.Owner,
        OwnerConfirmed = true,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static BusinessGoal Goal(Guid businessId, string type) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        Type = type,
        Title = "Owner goal",
        Priority = 1,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static ResolvedKnowledgeBundle RestaurantBundle(
        IReadOnlyList<ResolvedKnowledgeFact>? context = null,
        IReadOnlyList<ResolvedKnowledgeFact>? memory = null)
    {
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var restaurant = RestaurantCafeKnowledgeManifestV2.Create();
        return new ResolvedKnowledgeBundle(
            "restaurant-cafe",
            null,
            [
                new ResolvedKnowledgeManifest(core.Layer, core.PackKey, core.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(core)),
                new ResolvedKnowledgeManifest(restaurant.Layer, restaurant.PackKey, restaurant.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(restaurant))
            ],
            context ?? [],
            [],
            memory ?? [],
            "goal-map-bundle");
    }
}
