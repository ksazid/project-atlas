using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OpportunityDetailPolicyTests
{
    [Fact]
    public void Detail_separates_evidence_interpretation_and_limitations()
    {
        var goal = new BusinessGoal { Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), Type = "revenue", Title = "Increase repeat sales", Priority = 1, IsCustom = false, UpdatedAt = DateTimeOffset.UtcNow };
        var opportunity = Opportunity(goal.Id, DateTimeOffset.UtcNow.AddHours(2));

        var detail = OpportunityPolicy.Detail(opportunity, goal, DateTimeOffset.UtcNow);

        Assert.Equal(goal.Title, detail.GoalTitle);
        Assert.Contains(detail.Evidence, x => x.Category == "business-profile" && x.Source == "owner-confirmed");
        Assert.Contains(detail.Evidence, x => x.Category == "business-goal" && x.Value == goal.Title);
        Assert.NotEmpty(detail.Assumptions);
        Assert.NotEmpty(detail.Limitations);
        Assert.False(detail.ExecutionKitAvailable);
        Assert.False(detail.IsExpired);
    }

    [Fact]
    public void Detail_is_safe_when_recorded_evidence_is_malformed()
    {
        var opportunity = Opportunity(null, DateTimeOffset.UtcNow.AddHours(1));
        opportunity.EvidenceJson = "not-json";

        var detail = OpportunityPolicy.Detail(opportunity, null, DateTimeOffset.UtcNow);

        Assert.Single(detail.Evidence);
        Assert.Equal("summary", detail.Evidence[0].Category);
        Assert.Null(detail.GoalTitle);
    }

    [Fact]
    public void Detail_reports_expiry_without_changing_historical_pack_reference()
    {
        var opportunity = Opportunity(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-1));

        var detail = OpportunityPolicy.Detail(opportunity, null, DateTimeOffset.UtcNow);

        Assert.True(detail.IsExpired);
        Assert.Equal(OpportunityStatuses.Expired, detail.Status);
        Assert.Equal("generic-business", detail.KnowledgePackKey);
        Assert.Equal("1.0", detail.KnowledgePackVersion);
    }

    private static Opportunity Opportunity(Guid? goalId, DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), Title = "Review one practical action",
        WhyItMatters = "Supports the selected goal.", WhyNow = "Confirmed context is available.",
        ExpectedImpact = "Clarify a measurable next step.", Effort = "Low", Confidence = "Medium",
        EvidenceSummary = "Confirmed profile and goal.",
        EvidenceJson = "{\"profile\":\"owner-confirmed\",\"goal\":\"Increase repeat sales\",\"PackKey\":\"generic-business\"}",
        Status = OpportunityStatuses.Available, KnowledgePackKey = "generic-business", KnowledgePackVersion = "1.0",
        KnowledgePackVersionId = Guid.NewGuid(), GoalId = goalId, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5), ExpiresAt = expiresAt
    };
}
