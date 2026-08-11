using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessHubTests
{
    [Fact]
    public async Task BuildAsync_returns_bounded_media_menu_summary_and_context_for_owned_business()
    {
        await using var db = TestDb();
        var business = SeedOwnedBusiness(db, "owner-a");
        var observed = new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

        db.BusinessProfiles.Add(new BusinessProfile
        {
            BusinessId = business.Id,
            Description = "Turkish kebab restaurant",
            Address = "Valletta, Malta",
            Website = "https://hasans.example",
            Phone = "+356 2000 0000",
            Email = null,
            SocialChannels = null,
            BusinessHours = "Mon-Sun 11:00-23:00",
            Language = "English",
            Source = FieldSources.Public,
            OwnerConfirmed = true,
            UpdatedAt = observed
        });

        for (var i = 0; i < 8; i++)
            db.Set<BusinessMediaReference>().Add(Media(business.Id, i, $"https://cdn.example/{i}.jpg", observed.AddMinutes(i)));

        db.Set<BusinessOffering>().AddRange(
            Offering(business.Id, "Wraps & Pita", "Any Grill in Pita Bread", 9.50m, "EUR", observed),
            Offering(business.Id, "Beverages", "Ice Tea Peach", 2.50m, "EUR", observed.AddMinutes(1)),
            Offering(business.Id, "Beverages", "Water", 1.80m, "EUR", observed.AddMinutes(2)));

        db.BusinessContextEntries.AddRange(
            Context(business.Id, "service-style", "takeaway"),
            Context(business.Id, "customer-profile", "local and tourist"),
            Context(business.Id, "peak-period", "evening"),
            Context(business.Id, "differentiator", "Turkish grill"),
            Context(business.Id, "capacity", "small team"));

        await db.SaveChangesAsync();

        var result = await BusinessHubReader.BuildAsync(db, business.Id, "owner-a", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Hasan's Turkish Kebab House", result!.Business.Name);
        Assert.Equal(6, result.Media.Count);
        Assert.Equal(3, result.Menu.ItemCount);
        Assert.Equal(2, result.Menu.SectionCount);
        Assert.Equal(1.80m, result.Menu.MinPrice);
        Assert.Equal(9.50m, result.Menu.MaxPrice);
        Assert.Equal("EUR", result.Menu.Currency);
        Assert.Equal("strong", result.Context.Status);
        Assert.Equal(observed.AddMinutes(7), result.LatestObservedAt);
    }

    [Fact]
    public async Task BuildAsync_filters_invalid_and_duplicate_media_references()
    {
        await using var db = TestDb();
        var business = SeedOwnedBusiness(db, "owner-a");
        var observed = new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

        db.Set<BusinessMediaReference>().AddRange(
            Media(business.Id, 0, "https://cdn.example/hero.jpg", observed),
            Media(business.Id, 1, "https://cdn.example/HERO.jpg", observed.AddMinutes(1)),
            Media(business.Id, 2, "http://cdn.example/insecure.jpg", observed.AddMinutes(2)),
            Media(business.Id, 3, "not-a-url", observed.AddMinutes(3)));
        await db.SaveChangesAsync();

        var result = await BusinessHubReader.BuildAsync(db, business.Id, "owner-a", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!.Media);
        Assert.Equal("https://cdn.example/hero.jpg", result.Media[0].RemoteUrl);
    }

    [Fact]
    public async Task BuildAsync_returns_null_for_business_owned_by_another_subject()
    {
        await using var db = TestDb();
        var business = SeedOwnedBusiness(db, "owner-a");
        await db.SaveChangesAsync();

        Assert.Null(await BusinessHubReader.BuildAsync(db, business.Id, "owner-b", CancellationToken.None));
    }

    private static AtlasDbContext TestDb() => new(new DbContextOptionsBuilder<AtlasDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static Business SeedOwnedBusiness(AtlasDbContext db, string subject)
    {
        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            ProviderSubject = subject,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var business = Business.Create(new CreateBusinessRequest(
            "Hasan's Turkish Kebab House",
            "restaurant-cafe",
            "MT",
            "Europe/Malta",
            "EUR",
            "Valletta, Malta",
            "Open"));

        db.UserAccounts.Add(account);
        db.Businesses.Add(business);
        db.BusinessMemberships.Add(new BusinessMembership
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            UserAccountId = account.Id,
            UserAccount = account,
            Role = MembershipRoles.BusinessOwner,
            CreatedAt = DateTimeOffset.UtcNow
        });
        return business;
    }

    private static BusinessMediaReference Media(Guid businessId, int order, string url, DateTimeOffset observedAt) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        SourceOrder = order,
        Kind = "business-image",
        RemoteUrl = url,
        Source = "bolt-food",
        SourceUrl = "https://food.bolt.eu/example",
        ObservedAt = observedAt,
        Confidence = "high",
        EvidenceClass = "public-page",
        OwnerConfirmed = false,
        AltText = order == 0 ? "Hasan storefront" : null,
        CreatedAt = observedAt
    };

    private static BusinessOffering Offering(Guid businessId, string section, string name, decimal price, string currency, DateTimeOffset observedAt) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        SourceOrder = 0,
        Kind = "menu-item",
        Section = section,
        Name = name,
        Description = null,
        Price = price,
        Currency = currency,
        Source = "bolt-food",
        SourceUrl = "https://food.bolt.eu/example",
        ObservedAt = observedAt,
        Confidence = "high",
        EvidenceClass = "public-page",
        OwnerConfirmed = false,
        CreatedAt = observedAt
    };

    private static BusinessContextEntry Context(Guid businessId, string key, string value) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        Key = key,
        Value = value,
        Source = FieldSources.Owner,
        OwnerConfirmed = true,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}
