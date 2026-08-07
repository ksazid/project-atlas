using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class ExecutionKitPolicyTests
{
    [Theory]
    [InlineData(OpportunityStatuses.Available, true)]
    [InlineData(OpportunityStatuses.Applied, true)]
    [InlineData(OpportunityStatuses.Skipped, false)]
    [InlineData(OpportunityStatuses.NotRelevant, false)]
    [InlineData(OpportunityStatuses.Expired, false)]
    public void Eligibility_requires_current_actionable_opportunity(string status, bool expected)
    {
        var opportunity = Opportunity(status, DateTimeOffset.UtcNow.AddHours(1));
        Assert.Equal(expected, ExecutionKitPolicy.IsEligible(opportunity, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Expired_opportunity_is_not_eligible()
    {
        Assert.False(ExecutionKitPolicy.IsEligible(Opportunity(OpportunityStatuses.Available, DateTimeOffset.UtcNow.AddMinutes(-1)), DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void Usefulness_rating_is_bounded(int? rating, bool expected)
    {
        Assert.Equal(expected, ExecutionKitPolicy.IsValidRating(rating));
    }

    [Theory]
    [InlineData(ExecutionAssetTypes.Checklist, true)]
    [InlineData(ExecutionAssetTypes.MessageTemplate, true)]
    [InlineData(ExecutionAssetTypes.MeasurementSuggestion, true)]
    [InlineData("publish-command", false)]
    public void Asset_types_are_explicit(string type, bool expected)
    {
        Assert.Equal(expected, ExecutionAssetTypes.IsSupported(type));
    }

    private static Opportunity Opportunity(string status, DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = Guid.NewGuid(),
        Title = "Review a practical action",
        WhyItMatters = "Goal alignment",
        WhyNow = "Current context",
        ExpectedImpact = "Measured learning",
        Effort = "Low",
        Confidence = "Medium",
        EvidenceSummary = "Confirmed profile and goal",
        EvidenceJson = "{}",
        Status = status,
        KnowledgePackKey = KnowledgePackKeys.GenericBusiness,
        KnowledgePackVersion = "1.0",
        KnowledgePackVersionId = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow,
        ExpiresAt = expiresAt
    };
}
