using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class HistoryPolicyTests
{
    [Theory]
    [InlineData("completed", "completed", null, true)]
    [InlineData("completed", "applied", null, false)]
    [InlineData("completed", null, "knowledge-pack", true)]
    [InlineData("completed", null, "unknown", false)]
    public void Filters_are_deterministic(string currentStatus, string? status, string? category, bool expected)
    {
        var categories = new[] { "business-goal", "knowledge-pack" };
        Assert.Equal(expected, HistoryPolicy.Matches(currentStatus, categories, status, category));
    }

    [Theory]
    [InlineData(null, 50)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    [InlineData(500, 100)]
    public void Limit_is_bounded(int? requested, int expected)
    {
        Assert.Equal(expected, HistoryPolicy.ClampLimit(requested));
    }

    [Fact]
    public void Learning_summary_does_not_claim_causation_without_outcome()
    {
        var summary = HistoryPolicy.LearningSummary("completed", null, null);
        Assert.Contains("No outcome has been recorded", summary);
    }

    [Fact]
    public void Categories_fall_back_safely_for_invalid_evidence_json()
    {
        var opportunity = new Opportunity
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Title = "Test",
            WhyItMatters = "Reason",
            WhyNow = "Now",
            ExpectedImpact = "Directional",
            Effort = "Low",
            Confidence = "Medium",
            EvidenceSummary = "Recorded evidence",
            EvidenceJson = "not-json",
            Status = OpportunityStatuses.Available,
            KnowledgePackKey = "generic-business",
            KnowledgePackVersion = "1.0",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        Assert.Contains("recorded-evidence", HistoryPolicy.Categories(opportunity));
    }
}
