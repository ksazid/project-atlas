using Atlas.Api;

namespace Atlas.Api.Tests;

public sealed class ActionDecisionPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("available", "applied", true)]
    [InlineData("available", "skipped", true)]
    [InlineData("available", "not-relevant", true)]
    [InlineData("available", "rejected", true)]
    [InlineData("available", "completed", false)]
    [InlineData("applied", "completed", true)]
    [InlineData("applied", "skipped", true)]
    [InlineData("applied", "not-relevant", true)]
    [InlineData("applied", "rejected", true)]
    [InlineData("completed", "applied", false)]
    [InlineData("skipped", "applied", false)]
    [InlineData("not-relevant", "applied", false)]
    [InlineData("rejected", "applied", false)]
    public void Transition_policy_is_deterministic(string current, string next, bool expected)
    {
        Assert.Equal(expected, ActionDecisionPolicy.CanTransition(current, next, Now.AddHours(1), Now));
    }

    [Fact]
    public void Expired_available_action_cannot_transition()
    {
        Assert.False(ActionDecisionPolicy.CanTransition(OpportunityStatuses.Available, ActionStatuses.Applied, Now, Now));
    }

    [Theory]
    [InlineData("skipped")]
    [InlineData("not-relevant")]
    [InlineData("rejected")]
    public void Terminal_decisions_require_supported_reason(string status)
    {
        var request = new RecordActionDecisionRequest(status, null, null, 1);
        Assert.Contains(nameof(RecordActionDecisionRequest.ReasonCode), request.Validate().Keys);
    }

    [Fact]
    public void Other_reason_requires_owner_note()
    {
        var request = new RecordActionDecisionRequest(ActionStatuses.Rejected, ActionDecisionReasonCodes.Other, null, 1);
        Assert.Contains(nameof(RecordActionDecisionRequest.OwnerNote), request.Validate().Keys);
    }

    [Fact]
    public void Applied_does_not_accept_reason_code()
    {
        var request = new RecordActionDecisionRequest(ActionStatuses.Applied, ActionDecisionReasonCodes.TimingNotRight, null, 1);
        Assert.Contains(nameof(RecordActionDecisionRequest.ReasonCode), request.Validate().Keys);
    }
}
