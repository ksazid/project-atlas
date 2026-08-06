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
        var owner = Account("owner-a"); var otherOwner = Account("owner-b");
        var owned = Business.Create(new CreateBusinessRequest("Owned", "Cafe", "Malta", "Europe/Malta", "EUR", "Balzan", "Open"));
        var foreign = Business.Create(new CreateBusinessRequest("Foreign", "Cafe", "Malta", "Europe/Malta", "EUR", "Sliema", "Open"));
        db.AddRange(owner, otherOwner, owned, foreign, Membership(owner, owned), Membership(otherOwner, foreign));
        await db.SaveChangesAsync();

        var result = await db.Businesses.Where(b => db.BusinessMemberships.Any(m => m.BusinessId == b.Id && m.UserAccount.ProviderSubject == owner.ProviderSubject && m.Role == MembershipRoles.BusinessOwner)).SingleOrDefaultAsync(b => b.Id == foreign.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task Owner_can_read_their_own_business()
    {
        await using var db = CreateDb(); var owner = Account("owner-a");
        var business = Business.Create(new CreateBusinessRequest("Owned", "Cafe", "Malta", "Europe/Malta", "EUR", "Balzan", "Open"));
        db.AddRange(owner, business, Membership(owner, business)); await db.SaveChangesAsync();
        var result = await db.Businesses.Where(b => db.BusinessMemberships.Any(m => m.BusinessId == b.Id && m.UserAccount.ProviderSubject == owner.ProviderSubject && m.Role == MembershipRoles.BusinessOwner)).SingleOrDefaultAsync(b => b.Id == business.Id);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Profile_goals_and_context_remain_scoped_to_the_owned_business()
    {
        await using var db = CreateDb(); var owner = Account("owner-a"); var other = Account("owner-b");
        var owned = Business.Create(new CreateBusinessRequest("Owned", "Cafe", "Malta", "Europe/Malta", "EUR", "Balzan", "Open"));
        var foreign = Business.Create(new CreateBusinessRequest("Foreign", "Cafe", "Malta", "Europe/Malta", "EUR", "Sliema", "Open"));
        db.AddRange(owner, other, owned, foreign, Membership(owner, owned), Membership(other, foreign));
        db.BusinessProfiles.Add(new BusinessProfile { BusinessId = foreign.Id, Description = "private", Language = "English", Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = DateTimeOffset.UtcNow });
        db.BusinessGoals.Add(new BusinessGoal { Id = Guid.NewGuid(), BusinessId = foreign.Id, Type = "revenue", Title = "Private goal", Priority = 1, UpdatedAt = DateTimeOffset.UtcNow });
        db.BusinessContextEntries.Add(new BusinessContextEntry { Id = Guid.NewGuid(), BusinessId = foreign.Id, Key = "constraint", Value = "private", Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var ownedIds = db.BusinessMemberships.Where(m => m.UserAccount.ProviderSubject == owner.ProviderSubject && m.Role == MembershipRoles.BusinessOwner).Select(m => m.BusinessId);
        Assert.Empty(await db.BusinessProfiles.Where(x => ownedIds.Contains(x.BusinessId)).ToListAsync());
        Assert.Empty(await db.BusinessGoals.Where(x => ownedIds.Contains(x.BusinessId)).ToListAsync());
        Assert.Empty(await db.BusinessContextEntries.Where(x => ownedIds.Contains(x.BusinessId)).ToListAsync());
    }

    private static AtlasDbContext CreateDb() => new(new DbContextOptionsBuilder<AtlasDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static UserAccount Account(string subject) => new() { Id = Guid.NewGuid(), ProviderSubject = subject, CreatedAt = DateTimeOffset.UtcNow };
    private static BusinessMembership Membership(UserAccount account, Business business) => new() { Id = Guid.NewGuid(), BusinessId = business.Id, UserAccountId = account.Id, UserAccount = account, Role = MembershipRoles.BusinessOwner, CreatedAt = DateTimeOffset.UtcNow };
}
