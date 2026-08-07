using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OutcomePolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Completed_action_is_eligible_for_outcome_capture()
    {
        var opportunity = Opportunity(ActionStatuses.Completed);
        Assert.True(OutcomePolicy.CanCapture(opportunity, Now));
    }

    [Theory]
    [InlineData("available")]
    [InlineData("applied")]
    [InlineData("skipped")]
    [InlineData("not-relevant")]
    [InlineData("rejected")]
    public void Non_completed_action_is_not_eligible_for_outcome_capture(string status)
    {
        var opportunity = Opportunity(status);
        Assert.False(OutcomePolicy.CanCapture(opportunity, Now));
    }

    [Theory]
    [InlineData("measured")]
    [InlineData("owner-reported")]
    [InlineData("estimated")]
    [InlineData("unknown")]
    public void Supported_evidence_classes_validate(string evidenceClass)
    {
        var request = ValidRequest(evidenceClass);
        Assert.Empty(request.Validate(Now));
    }

    [Fact]
    public void Measured_outcome_requires_measure_name_and_value()
    {
        var request = new UpsertOutcomeRequest(4, "More enquiries", 20, null, null, null, null, OutcomeEvidenceClasses.Measured, null, null);
        Assert.Contains(nameof(UpsertOutcomeRequest.MeasureValue), request.Validate(Now).Keys);
    }

    [Fact]
    public void Follow_up_cannot_be_in_the_past()
    {
        var request = ValidRequest(OutcomeEvidenceClasses.OwnerReported) with { FollowUpAt = Now.AddHours(-1) };
        Assert.Contains(nameof(UpsertOutcomeRequest.FollowUpAt), request.Validate(Now).Keys);
    }

    [Fact]
    public void Usefulness_is_bounded_to_five_point_scale()
    {
        var request = ValidRequest(OutcomeEvidenceClasses.Unknown) with { UsefulnessRating = 6 };
        Assert.Contains(nameof(UpsertOutcomeRequest.UsefulnessRating), request.Validate(Now).Keys);
    }

    private static UpsertOutcomeRequest ValidRequest(string evidenceClass) => new(
        4,
        "Owner observed a useful result.",
        20,
        "Optional context",
        evidenceClass == OutcomeEvidenceClasses.Measured ? "Bookings" : null,
        evidenceClass == OutcomeEvidenceClasses.Measured ? 3 : null,
        evidenceClass == OutcomeEvidenceClasses.Measured ? "count" : null,
        evidenceClass,
        Now.AddDays(7),
        null);

    private static Opportunity Opportunity(string status) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = Guid.NewGuid(),
        Title = "Test action",
        WhyItMatters = "Supports the goal",
        WhyNow = "Timing matters",
        ExpectedImpact = "Directional improvement",
        Effort = "Low",
        Confidence = "Medium",
        EvidenceSummary = "Owner-confirmed context",
        EvidenceJson = "{}",
        Status = status,
        KnowledgePackKey = "generic-business",
        KnowledgePackVersion = "1.0",
        KnowledgePackVersionId = Guid.NewGuid(),
        CreatedAt = Now.AddDays(-1),
        ExpiresAt = Now.AddDays(1)
    };
}
