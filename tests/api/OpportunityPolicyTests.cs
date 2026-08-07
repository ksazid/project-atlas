using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OpportunityPolicyTests
{
    [Fact]
    public void Eligibility_requires_confirmed_profile_goal_and_current_pack_assignment()
    {
        var profile = new BusinessProfile
        {
            BusinessId = Guid.NewGuid(), Language = "en", Source = FieldSources.Owner,
            OwnerConfirmed = true, UpdatedAt = DateTimeOffset.UtcNow
        };
        var goals = new[]
        {
            new BusinessGoal { Id = Guid.NewGuid(), BusinessId = profile.BusinessId, Type = "growth", Title = "Increase retention", Priority = 1, UpdatedAt = DateTimeOffset.UtcNow }
        };
        var assignment = new BusinessKnowledgeAssignment { Id = Guid.NewGuid(), BusinessId = profile.BusinessId, PackKey = "generic-business", ExactVersion = "1.0", IsCurrent = true };

        Assert.True(OpportunityPolicy.IsEligible(profile, goals, assignment));
        profile.OwnerConfirmed = false;
        Assert.False(OpportunityPolicy.IsEligible(profile, goals, assignment));
        profile.OwnerConfirmed = true;
        Assert.False(OpportunityPolicy.IsEligible(profile, [], assignment));
        assignment.IsCurrent = false;
        Assert.False(OpportunityPolicy.IsEligible(profile, goals, assignment));
    }

    [Fact]
    public void Expired_or_decided_opportunities_cannot_be_actioned()
    {
        var now = DateTimeOffset.UtcNow;
        var opportunity = Opportunity(now.AddHours(1));
        Assert.True(OpportunityPolicy.CanDecide(opportunity, now));

        opportunity.ExpiresAt = now;
        Assert.False(OpportunityPolicy.CanDecide(opportunity, now));
        Assert.Equal(OpportunityStatuses.Expired, OpportunityPolicy.StatusFor(opportunity, now));

        opportunity.ExpiresAt = now.AddHours(1);
        opportunity.Status = OpportunityStatuses.Applied;
        Assert.False(OpportunityPolicy.CanDecide(opportunity, now));
    }

    [Theory]
    [InlineData(OpportunityDecisions.Apply, null, true)]
    [InlineData(OpportunityDecisions.Skip, "Not now", true)]
    [InlineData(OpportunityDecisions.NotRelevant, "Wrong fit", true)]
    [InlineData(OpportunityDecisions.Skip, null, false)]
    [InlineData("unknown", null, false)]
    public void Decision_validation_is_explicit(string decision, string? reason, bool valid)
    {
        var request = new OpportunityDecisionRequest(decision, reason, 1);
        Assert.Equal(valid, request.Validate().Count == 0);
    }

    private static Opportunity Opportunity(DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), Title = "Review next action",
        WhyItMatters = "Supports a priority goal", WhyNow = "Context is confirmed",
        ExpectedImpact = "Clarify next step", Effort = "Low", Confidence = "Medium",
        EvidenceSummary = "Confirmed profile and goal", EvidenceJson = "{}",
        Status = OpportunityStatuses.Available, KnowledgePackKey = "generic-business",
        KnowledgePackVersion = "1.0", KnowledgePackVersionId = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = expiresAt
    };
}
