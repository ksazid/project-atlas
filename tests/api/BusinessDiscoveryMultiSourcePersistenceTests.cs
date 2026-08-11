using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryMultiSourcePersistenceTests
{
    [Fact]
    public void DiscoverySource_MapsToMigratedPluralTableName()
    {
        using var db = new AtlasDbContext(
            new DbContextOptionsBuilder<AtlasDbContext>()
                .UseNpgsql("Host=localhost;Port=5432;Database=atlas;Username=postgres;Password=postgres")
                .Options);

        var tableName = db.Model.FindEntityType(typeof(BusinessDiscoverySource))?.GetTableName();

        Assert.Equal("BusinessDiscoverySources", tableName);
    }

    [Fact]
    public async Task Snapshot_PersistsOrderedSourcesAndReconciliationEvidence_WithoutDuplicatingSelectedFacts()
    {
        await using var db = new AtlasDbContext(
            new DbContextOptionsBuilder<AtlasDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var accountId = Guid.NewGuid();
        var observedAt = new DateTimeOffset(2026, 8, 11, 2, 20, 0, TimeSpan.Zero);
        var reconciliation = BusinessDiscoveryReconciler.Reconcile([
            Source(0, true, "website", "https://restaurant.example/menu", observedAt,
                ("name", "Antalya Kebab St. Julian's"),
                ("phone", "+356 2100 0000")),
            Source(1, false, "bolt-food", "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians", observedAt.AddMinutes(1),
                ("name", "Antalya Kebab St. Julian's"),
                ("description", "Turkish kebab restaurant")),
            Source(2, false, "wolt", "https://wolt.com/en/mlt/malta/restaurant/antalya-kebab", observedAt.AddMinutes(2),
                ("name", "Antalya Kebab St. Julian's"),
                ("phone", "+356 7999 9999"))
        ]);

        var snapshot = BusinessDiscoverySnapshot.Create(accountId, reconciliation);
        db.BusinessDiscoverySnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var stored = await db.BusinessDiscoverySnapshots
            .Include(x => x.Facts)
            .Include(x => x.Sources)
            .Include(x => x.Evidence)
            .SingleAsync(x => x.Id == snapshot.Id);

        Assert.Equal(3, stored.Sources.Count);
        Assert.Equal([0, 1, 2], stored.Sources.OrderBy(x => x.Order).Select(x => x.Order).ToArray());
        Assert.True(stored.Sources.Single(x => x.Order == 0).IsPrimary);
        Assert.Equal("website", stored.Sources.Single(x => x.Order == 0).Provider);
        Assert.Equal("bolt-food", stored.Sources.Single(x => x.Order == 1).Provider);
        Assert.Equal(observedAt.AddMinutes(2), stored.Sources.Single(x => x.Order == 2).ObservedAt);

        Assert.Single(stored.Facts.Where(x => x.Key == "name"));
        Assert.Single(stored.Facts.Where(x => x.Key == "phone"));
        Assert.Contains(stored.Evidence, x => x.SourceOrder == 0 && x.Key == "name" && x.ReconciliationState == "selected");
        Assert.Contains(stored.Evidence, x => x.SourceOrder == 1 && x.Key == "name" && x.ReconciliationState == "corroborating");
        Assert.Contains(stored.Evidence, x => x.SourceOrder == 2 && x.Key == "phone" && x.ReconciliationState == "conflict");
    }

    [Fact]
    public void LegacySingleSourceFactory_RemainsSupportedAndCreatesAuditablePrimarySource()
    {
        var accountId = Guid.NewGuid();
        var observedAt = new DateTimeOffset(2026, 8, 11, 2, 20, 0, TimeSpan.Zero);
        var publicSnapshot = new PublicBusinessSnapshot(
            "website",
            "https://restaurant.example/menu",
            observedAt,
            [new PublicBusinessFact("name", "Antalya Kebab", "website", "https://restaurant.example/menu", observedAt, "high")]);

        var snapshot = BusinessDiscoverySnapshot.Create(accountId, publicSnapshot);

        Assert.Single(snapshot.Sources);
        Assert.True(snapshot.Sources.Single().IsPrimary);
        Assert.Single(snapshot.Evidence);
        Assert.Equal("selected", snapshot.Evidence.Single().ReconciliationState);
    }

    private static BusinessSourceObservation Source(
        int order,
        bool primary,
        string provider,
        string url,
        DateTimeOffset at,
        params (string Key, string Value)[] values) =>
        new(
            order,
            primary,
            provider,
            url,
            "success",
            values.Select(value => new PublicBusinessFact(
                value.Key,
                value.Value,
                provider,
                url,
                at,
                "high")).ToList());
}
