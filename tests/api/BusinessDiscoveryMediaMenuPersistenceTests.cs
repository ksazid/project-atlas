using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryMediaMenuPersistenceTests
{
    [Fact]
    public async Task Snapshot_persists_public_media_and_offerings_with_provenance()
    {
        await using var db = Db();
        var accountId = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 8, 11, 13, 0, 0, TimeSpan.Zero);
        var sourceUrl = "https://restaurant.example.com/menu";
        var snapshot = BusinessDiscoverySnapshot.Create(accountId, Snapshot(sourceUrl, at));

        db.BusinessDiscoverySnapshots.Add(snapshot);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var stored = await db.BusinessDiscoverySnapshots
            .Include(x => x.Media)
            .Include(x => x.Offerings)
            .SingleAsync(x => x.Id == snapshot.Id);

        var image = Assert.Single(stored.Media);
        Assert.Equal("https://cdn.example.com/hero.jpg", image.RemoteUrl);
        Assert.Equal("business-image", image.Kind);
        Assert.Equal("website", image.Source);
        Assert.Equal(sourceUrl, image.SourceUrl);
        Assert.Equal(at, image.ObservedAt);
        Assert.Equal("public-observed", image.EvidenceClass);
        Assert.False(image.OwnerConfirmed);

        var offering = Assert.Single(stored.Offerings);
        Assert.Equal("menu-item", offering.Kind);
        Assert.Equal("Kebabs", offering.Section);
        Assert.Equal("Chicken Kebab", offering.Name);
        Assert.Equal(12.50m, offering.Price);
        Assert.Equal("EUR", offering.Currency);
        Assert.Equal("website", offering.Source);
        Assert.False(offering.OwnerConfirmed);
    }

    [Fact]
    public async Task Confirming_discovery_materialises_business_media_and_offerings_without_false_owner_confirmation()
    {
        await using var db = Db();
        const string subject = "vs25-owner";
        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            ProviderSubject = subject,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var at = new DateTimeOffset(2026, 8, 11, 13, 0, 0, TimeSpan.Zero);
        var snapshot = BusinessDiscoverySnapshot.Create(account.Id, Snapshot("https://restaurant.example.com/menu", at));
        db.UserAccounts.Add(account);
        db.BusinessDiscoverySnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var business = await BusinessDiscoveryBusinessCreator.CreateAsync(
            db,
            subject,
            new CreateBusinessFromDiscoveryRequest(
                snapshot.Id,
                "Atlas Kitchen",
                "restaurant-cafe",
                "restaurant",
                "MT",
                "Europe/Malta",
                "EUR",
                "Birkirkara, Malta",
                "Open",
                "Turkish restaurant",
                "https://restaurant.example.com/",
                "+35620000000",
                "Mo-Su 11:00-22:00",
                "en",
                true),
            CancellationToken.None);

        var image = await db.Set<BusinessMediaReference>().SingleAsync(x => x.BusinessId == business.Id);
        Assert.Equal("https://cdn.example.com/hero.jpg", image.RemoteUrl);
        Assert.Equal("website", image.Source);
        Assert.Equal("public-observed", image.EvidenceClass);
        Assert.False(image.OwnerConfirmed);

        var offering = await db.Set<BusinessOffering>().SingleAsync(x => x.BusinessId == business.Id);
        Assert.Equal("menu-item", offering.Kind);
        Assert.Equal("Chicken Kebab", offering.Name);
        Assert.Equal(12.50m, offering.Price);
        Assert.Equal("EUR", offering.Currency);
        Assert.False(offering.OwnerConfirmed);

        var consumed = await db.BusinessDiscoverySnapshots.SingleAsync(x => x.Id == snapshot.Id);
        Assert.Equal(business.Id, consumed.BusinessId);
        Assert.NotNull(consumed.ConsumedAt);
    }

    [Fact]
    public void Media_and_offering_entities_map_to_explicit_plural_tables()
    {
        using var db = new AtlasDbContext(
            new DbContextOptionsBuilder<AtlasDbContext>()
                .UseNpgsql("Host=localhost;Port=5432;Database=atlas;Username=postgres;Password=postgres")
                .Options);

        Assert.Equal("BusinessDiscoveryMediaReferences", db.Model.FindEntityType(typeof(BusinessDiscoveryMediaReference))?.GetTableName());
        Assert.Equal("BusinessDiscoveryOfferings", db.Model.FindEntityType(typeof(BusinessDiscoveryOffering))?.GetTableName());
        Assert.Equal("BusinessMediaReferences", db.Model.FindEntityType(typeof(BusinessMediaReference))?.GetTableName());
        Assert.Equal("BusinessOfferings", db.Model.FindEntityType(typeof(BusinessOffering))?.GetTableName());
    }

    private static AtlasDbContext Db() => new(
        new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static PublicBusinessSnapshot Snapshot(string sourceUrl, DateTimeOffset at) => new(
        "website",
        sourceUrl,
        at,
        [
            new PublicBusinessFact("name", "Atlas Kitchen", "website", sourceUrl, at, "high"),
            new PublicBusinessFact("category", "restaurant-cafe", "website", sourceUrl, at, "high"),
            new PublicBusinessFact("primaryLocation", "Birkirkara, Malta", "website", sourceUrl, at, "high")
        ])
    {
        Media = [new PublicBusinessMedia(
            "business-image",
            "https://cdn.example.com/hero.jpg",
            "website",
            sourceUrl,
            at,
            "high")],
        Offerings = [new PublicBusinessOffering(
            "menu-item",
            "Kebabs",
            "Chicken Kebab",
            "Chargrilled chicken",
            12.50m,
            "EUR",
            "website",
            sourceUrl,
            at,
            "high")]
    };
}
