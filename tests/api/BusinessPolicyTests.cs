using Atlas.Api;

namespace Atlas.Api.Tests;

public sealed class BusinessPolicyTests
{
    [Fact]
    public void Create_business_requires_all_minimum_fields()
    {
        var request = new CreateBusinessRequest("", "", "", "", "EU", "", "");
        var errors = request.Validate();
        Assert.Contains(nameof(request.Name), errors.Keys);
        Assert.Contains(nameof(request.Currency), errors.Keys);
        Assert.True(errors.Count >= 7);
    }

    [Fact]
    public void Business_normalizes_owner_supplied_values()
    {
        var business = Business.Create(new CreateBusinessRequest(" Atlas Cafe ", " Cafe ", " Malta ", " Europe/Malta ", "eur", " Balzan ", "Open"));
        Assert.Equal("Atlas Cafe", business.Name);
        Assert.Equal("EUR", business.Currency);
        Assert.NotEqual(Guid.Empty, business.Id);
    }

    [Fact]
    public void Internal_roles_are_distinct_from_customer_owner_role()
    {
        Assert.NotEqual(MembershipRoles.BusinessOwner, MembershipRoles.PilotOperator);
        Assert.NotEqual(MembershipRoles.BusinessOwner, MembershipRoles.PlatformAdministrator);
    }
}
