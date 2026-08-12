using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class PilotOwnerReconfirmationTests
{
    [Fact]
    public void Operator_assisted_profile_requires_owner_reconfirmation_and_existing_provenance()
    {
        var unconfirmed = new UpsertBusinessProfileRequest(
            "Updated by pilot support", null, null, null, null, null, null,
            "en", FieldSources.OperatorAssisted, false);
        var confirmed = unconfirmed with { OwnerConfirmed = true };

        var unconfirmedErrors = unconfirmed.Validate(FieldSources.OperatorAssisted);
        Assert.Contains(nameof(UpsertBusinessProfileRequest.OwnerConfirmed), unconfirmedErrors.Keys);

        var confirmedErrors = confirmed.Validate(FieldSources.OperatorAssisted);
        Assert.Empty(confirmedErrors);

        var spoofedErrors = confirmed.Validate(FieldSources.Owner);
        Assert.Contains(nameof(UpsertBusinessProfileRequest.Source), spoofedErrors.Keys);
    }
}
