using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class PilotOwnerReconfirmationTests
{
    [Fact]
    public void Operator_assisted_profile_requires_owner_reconfirmation()
    {
        var unconfirmed = new UpsertBusinessProfileRequest(
            "Updated by pilot support", null, null, null, null, null, null,
            "en", FieldSources.OperatorAssisted, false);
        var confirmed = unconfirmed with { OwnerConfirmed = true };

        var unconfirmedErrors = unconfirmed.Validate();
        Assert.Contains(nameof(UpsertBusinessProfileRequest.OwnerConfirmed), unconfirmedErrors.Keys);

        var confirmedErrors = confirmed.Validate();
        Assert.Empty(confirmedErrors);
    }
}
