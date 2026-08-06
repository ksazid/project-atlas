using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessProfilePolicyTests
{
    [Fact]
    public void Public_profile_data_requires_owner_confirmation()
    {
        var request = new UpsertBusinessProfileRequest(null, null, null, null, null, null, null, "en", FieldSources.Public, false);
        var errors = request.Validate();
        Assert.Contains(nameof(request.OwnerConfirmed), errors.Keys);
    }

    [Fact]
    public void Owner_profile_data_can_be_saved_without_public_confirmation()
    {
        var request = new UpsertBusinessProfileRequest("Cafe", "Balzan", null, null, null, null, null, "en", FieldSources.Owner, false);
        Assert.Empty(request.Validate());
    }

    [Fact]
    public void Goal_priorities_must_be_unique()
    {
        var request = new UpsertGoalsRequest([
            new GoalInput("revenue", "Grow revenue", 1, false),
            new GoalInput("retention", "Retain customers", 1, false)
        ]);
        Assert.Contains(nameof(request.Goals), request.Validate().Keys);
    }

    [Fact]
    public void A_custom_goal_is_supported()
    {
        var request = new UpsertGoalsRequest([new GoalInput("custom", "Reduce owner admin time", 1, true)]);
        Assert.Empty(request.Validate());
    }
}
