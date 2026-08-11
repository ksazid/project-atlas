using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OpportunityGenerationDetailTests
{
    [Fact]
    public void Detail_exposes_structured_evidence_goal_alignment_assumptions_and_limitations()
    {
        var now = new DateTimeOffset(2026, 8, 11, 2, 30, 0, TimeSpan.Zero);
        var businessId = Guid.NewGuid();
        var goal = new BusinessGoal
        {
            Id = Guid.NewGuid(), BusinessId = businessId, Type = "revenue", Title = "Increase revenue",
            Priority = 1, UpdatedAt = now
        };
        var profile = new BusinessProfile
        {
            BusinessId = businessId, Language = "en", Source = FieldSources.Owner,
            OwnerConfirmed = true, UpdatedAt = now
        };
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var restaurant = RestaurantCafeKnowledgeManifestV2.Create();
        var bundle = new ResolvedKnowledgeBundle(
            "restaurant-cafe",
            null,
            [
                new ResolvedKnowledgeManifest(core.Layer, core.PackKey, core.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(core)),
                new ResolvedKnowledgeManifest(restaurant.Layer, restaurant.PackKey, restaurant.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(restaurant))
            ],
            [new ResolvedKnowledgeFact("context", "primarychannels", "Takeaway", FieldSources.Owner)],
            [],
            [],
            "detail-bundle-fingerprint");
        var generated = OpportunityGenerator.Generate(profile, [goal], bundle, [], now).Selected!;
        var opportunity = new Opportunity
        {
            Id = Guid.NewGuid(), BusinessId = businessId, GoalId = goal.Id,
            Title = generated.Title, WhyItMatters = generated.Reason, WhyNow = generated.WhyNow,
            ExpectedImpact = generated.ExpectedImpact, Effort = generated.Effort, Confidence = generated.Confidence,
            EvidenceSummary = "Structured evidence available.", EvidenceJson = OpportunityGenerationSnapshot.Serialize(generated),
            Status = OpportunityStatuses.Available, KnowledgePackKey = generated.KnowledgePackKey,
            KnowledgePackVersion = generated.KnowledgePackVersion, KnowledgePackVersionId = Guid.NewGuid(),
            CreatedAt = now, ExpiresAt = now.AddHours(12)
        };

        var detail = OpportunityPolicy.Detail(opportunity, goal, now.AddMinutes(1));

        Assert.Equal(generated.GoalAlignment, detail.GoalAlignment);
        Assert.Equal(goal.Title, detail.GoalTitle);
        Assert.Contains(detail.Evidence, x =>
            x.Category == "context" && x.Label == "Primary channels" && x.Value == "Takeaway" && x.Source == FieldSources.Owner);
        Assert.DoesNotContain(detail.Evidence, x => x.Category == "summary");
        Assert.Equal(generated.Assumptions, detail.Assumptions);
        Assert.Equal(generated.Limitations, detail.Limitations);
        Assert.Contains("context", detail.SourceCategories);
        Assert.Equal($"Review and apply the proposed action: {generated.Title}", detail.ActionSummary);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.PackKey, detail.KnowledgePackKey);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.Version, detail.KnowledgePackVersion);
    }

    [Fact]
    public void Detail_uses_recorded_goal_alignment_when_goal_record_was_later_removed()
    {
        var now = DateTimeOffset.UtcNow;
        var evidenceJson = """
        {
          "schemaVersion": 1,
          "patternKey": "ordering-path-clarity-review",
          "bundleFingerprint": "abc",
          "goal": {
            "id": "11111111-1111-1111-1111-111111111111",
            "type": "revenue",
            "title": "Increase revenue",
            "priority": 1,
            "alignment": "Aligned to priority #1: Increase revenue"
          },
          "manifests": [],
          "evidence": [
            { "evidenceId": "e1", "layer": "context", "key": "primarychannels", "value": "Takeaway", "source": "owner" }
          ],
          "assumptions": ["Recorded assumption"],
          "limitations": ["Recorded limitation"]
        }
        """;
        var opportunity = BaseOpportunity(evidenceJson, now);

        var detail = OpportunityPolicy.Detail(opportunity, null, now);

        Assert.Equal("Aligned to priority #1: Increase revenue", detail.GoalAlignment);
        Assert.Equal("Increase revenue", detail.GoalTitle);
        Assert.Equal(["Recorded assumption"], detail.Assumptions);
        Assert.Equal(["Recorded limitation"], detail.Limitations);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"goal\":{\"unexpected\":true}}")]
    public void Malformed_or_legacy_incompatible_evidence_degrades_to_summary_without_throwing(string evidenceJson)
    {
        var now = DateTimeOffset.UtcNow;
        var opportunity = BaseOpportunity(evidenceJson, now);

        var detail = OpportunityPolicy.Detail(opportunity, null, now);

        var evidence = Assert.Single(detail.Evidence);
        Assert.Equal("summary", evidence.Category);
        Assert.Equal(opportunity.EvidenceSummary, evidence.Value);
    }

    private static Opportunity BaseOpportunity(string evidenceJson, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), Title = "Review ordering path",
        WhyItMatters = "Reason", WhyNow = "Why now", ExpectedImpact = "Directional impact", Effort = "Low",
        Confidence = "Medium", EvidenceSummary = "Recorded fallback evidence", EvidenceJson = evidenceJson,
        Status = OpportunityStatuses.Available, KnowledgePackKey = "restaurant-cafe", KnowledgePackVersion = "1.0",
        KnowledgePackVersionId = Guid.NewGuid(), CreatedAt = now, ExpiresAt = now.AddHours(1)
    };
}
