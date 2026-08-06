using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessIsolationTests
{
    [Fact]
    public async Task Owner_cannot_read_another_business()
    {
        await using var db = CreateDb();
        var owner = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "owner-a", CreatedAt = DateTimeOffset.UtcNow };
        var otherOwner = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "owner-b", CreatedAt = DateTimeOffset.UtcNow };
        var owned = Business.Create(new CreateBusinessRequest("Owned", "Cafe", "Malta", "Europe/Malta", "EUR", "Balzan", "Open"));
        var foreign = Business.Create(new CreateBusinessRequest("Foreign", "Cafe", "Malta", "Europe/Malta", "EUR", "Sliema", "Open"));

        db.AddRange(owner, otherOwner, owned, foreign);
        db.BusinessMemberships.AddRange(
            Membership(owner, owned),
            Membership(otherOwner, foreign));
        await db.SaveChangesAsync();

        var result = await db.Businesses
            .Where(b => db.BusinessMemberships.Any(m =>
                m.BusinessId == b.Id &&
                m.UserAccount.ProviderSubject == owner.ProviderSubject &&
                m.Role == MembershipRoles.BusinessOwner))
            .SingleOrDefaultAsync(b => b.Id == foreign.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Owner_can_read_their_own_business()
    {
        await using var db = CreateDb();
        var owner = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "owner-a", CreatedAt = DateTimeOffset.UtcNow };
        var business = Business.Create(new CreateBusinessRequest("Owned", "Cafe", "Malta", "Europe/Malta", "EUR", "Balzan", "Open"));
        db.AddRange(owner, business, Membership(owner, business));
        await db.SaveChangesAsync();

        var result = await db.Businesses
            .Where(b => db.BusinessMemberships.Any(m =>
                m.BusinessId == b.Id &&
                m.UserAccount.ProviderSubject == owner.ProviderSubject &&
                m.Role == MembershipRoles.BusinessOwner))
            .SingleOrDefaultAsync(b => b.Id == business.Id);

        Assert.NotNull(result);
        Assert.Equal(business.Id, result.Id);
    }

    private static AtlasDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AtlasDbContext(options);
    }

    private static BusinessMembership Membership(UserAccount account, Business business) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = business.Id,
        UserAccountId = account.Id,
        UserAccount = account,
        Role = MembershipRoles.BusinessOwner,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
