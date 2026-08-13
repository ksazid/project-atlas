using System.Text.Json;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OpportunityCooldownLegacyVs37Tests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 0, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Complete_schema_v1_same_evidence_suppresses_inside_cooldown()
    {
        var businessId = Guid.NewGuid();
        var goal = Goal(businessId);
        var bundle = Bundle("Improve weekday lunch demand", "same-bundle");
        var candidate = OfferCandidate(businessId, goal, bundle, Now.AddHours(-1));
        var prior = PriorOpportunity(businessId, candidate, LegacySnapshot(candidate), Now.AddHours(-1));

        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], bundle, [prior], Now);

        Assert.DoesNotContain(result.Candidates, item => item.PatternKey == "current-offer-visibility-review");
    }

    [Fact]
    public void Complete_schema_v1_changed_relevant_evidence_can_reconsider_inside_cooldown()
    {
        var businessId = Guid.NewGuid();
        var goal = Goal(businessId);
        var oldBundle = Bundle("Demand", "old-bundle");
        var oldCandidate = OfferCandidate(businessId, goal, oldBundle, Now.AddHours(-1));
        var prior = PriorOpportunity(businessId, oldCandidate, LegacySnapshot(oldCandidate), Now.AddHours(-1));
        var newBundle = Bundle("Improve weekday lunch and early-week demand", "new-bundle");

        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], newBundle, [prior], Now);

        Assert.Contains(result.Candidates, item => item.PatternKey == "current-offer-visibility-review");
    }

    [Fact]
    public void Incomplete_schema_v1_same_pattern_remains_conservatively_suppressed()
    {
        var businessId = Guid.NewGuid();
        var goal = Goal(businessId);
        var bundle = Bundle("Improve weekday lunch demand", "current-bundle");
        var candidate = OfferCandidate(businessId, goal, bundle, Now.AddHours(-1));
        var prior = PriorOpportunity(
            businessId,
            candidate,
            JsonSerializer.Serialize(new { schemaVersion = 1, patternKey = candidate.PatternKey }),
            Now.AddHours(-1));

        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], bundle, [prior], Now);

        Assert.DoesNotContain(result.Candidates, item => item.PatternKey == "current-offer-visibility-review");
    }

    private static GeneratedOpportunityCandidate OfferCandidate(
        Guid businessId,
        BusinessGoal goal,
        ResolvedKnowledgeBundle bundle,
        DateTimeOffset now) =>
        Assert.Single(
            OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], bundle, [], now).Candidates,
            item => item.PatternKey == "current-offer-visibility-review");

    private static string LegacySnapshot(GeneratedOpportunityCandidate candidate) => JsonSerializer.Serialize(new
    {
        schemaVersion = 1,
        patternKey = candidate.PatternKey,
        goal = new
        {
            id = candidate.GoalId,
            type = candidate.GoalType,
            title = candidate.GoalTitle,
            priority = candidate.GoalPriority
        },
        evidence = candidate.Evidence.Select(item => new { evidenceId = item.EvidenceId }).ToArray()
    });

    private static Opportunity PriorOpportunity(
        Guid businessId,
        GeneratedOpportunityCandidate candidate,
        string evidenceJson,
        DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        Title = candidate.Title,
        WhyItMatters = candidate.Reason,
        WhyNow = candidate.WhyNow,
        ExpectedImpact = candidate.ExpectedImpact,
        Effort = candidate.Effort,
        Confidence = candidate.Confidence,
        EvidenceSummary = "Prior evidence",
        EvidenceJson = evidenceJson,
        Status = OpportunityStatuses.Applied,
        KnowledgePackKey = candidate.KnowledgePackKey,
        KnowledgePackVersion = candidate.KnowledgePackVersion,
        KnowledgePackVersionId = Guid.NewGuid(),
        GoalId = candidate.GoalId,
        CreatedAt = createdAt,
        ExpiresAt = createdAt.AddDays(1)
    };

    private static BusinessProfile ConfirmedProfile(Guid businessId) => new()
    {
        BusinessId = businessId,
        Language = "en",
        Source = FieldSources.Owner,
        OwnerConfirmed = true,
        UpdatedAt = Now
    };

    private static BusinessGoal Goal(Guid businessId) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        Type = "revenue",
        Title = "Increase revenue",
        Priority = 1,
        UpdatedAt = Now
    };

    private static ResolvedKnowledgeBundle Bundle(string currentPriorities, string fingerprint)
    {
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var restaurant = RestaurantCafeKnowledgeManifestV2.Create();
        return new ResolvedKnowledgeBundle(
            "restaurant-cafe",
            null,
            [
                new ResolvedKnowledgeManifest(
                    core.Layer,
                    core.PackKey,
                    core.ExactVersion,
                    KnowledgePackManifestV2Policy.Fingerprint(core)),
                new ResolvedKnowledgeManifest(
                    restaurant.Layer,
                    restaurant.PackKey,
                    restaurant.ExactVersion,
                    KnowledgePackManifestV2Policy.Fingerprint(restaurant))
            ],
            [new ResolvedKnowledgeFact("context", "currentpriorities", currentPriorities, FieldSources.Owner)],
            [],
            [],
            fingerprint);
    }
}
