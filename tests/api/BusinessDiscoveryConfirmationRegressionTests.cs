using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryConfirmationRegressionTests
{
    [Fact]
    public async Task Confirming_a_multi_source_snapshot_creates_business_and_preserves_provenance()
    {
        await using var db = new AtlasDbContext(new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var owner = new UserAccount
        {
            Id = Guid.NewGuid(),
            ProviderSubject = "owner-multi-source",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var observedAt = new DateTimeOffset(2026, 8, 11, 4, 0, 0, TimeSpan.Zero);
        var primary = new BusinessSourceObservation(
            0, true, "bolt-food", "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians", "success",
            [
                Fact("name", "Antalya Kebab St. Julian's", "bolt-food", "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians", observedAt),
                Fact("category", "restaurant-cafe", "bolt-food", "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians", observedAt),
                Fact("description", "Turkish kebab restaurant", "bolt-food", "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians", observedAt)
            ]);
        var secondary = new BusinessSourceObservation(
            1, false, "website", "https://antalya.example", "success",
            [
                Fact("name", "Antalya Kebab St. Julian's", "website", "https://antalya.example", observedAt.AddSeconds(1)),
                Fact("category", "restaurant-cafe", "website", "https://antalya.example", observedAt.AddSeconds(1))
            ]);
        var reconciliation = BusinessDiscoveryReconciler.Reconcile([primary, secondary]);
        var snapshot = BusinessDiscoverySnapshot.Create(owner.Id, reconciliation);
        var (pack, _) = GenericBusinessKnowledgePack.Create(owner.Id);

        db.UserAccounts.Add(owner);
        db.BusinessDiscoverySnapshots.Add(snapshot);
        db.KnowledgePacks.Add(pack);
        await db.SaveChangesAsync();

        var request = new CreateBusinessFromDiscoveryRequest(
            snapshot.Id,
            "Antalya Kebab St. Julian's",
            "restaurant-cafe",
            "takeaway",
            "MT",
            "Europe/Malta",
            "EUR",
            "St Julian's, Malta",
            "Open",
            "Turkish kebab restaurant",
            "https://antalya.example",
            null,
            null,
            "English",
            true);

        var created = await BusinessDiscoveryBusinessCreator.CreateAsync(
            db, owner.ProviderSubject, request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.Id);
        var persisted = await db.BusinessDiscoverySnapshots
            .Include(x => x.Sources)
            .Include(x => x.Evidence)
            .SingleAsync(x => x.Id == snapshot.Id);
        Assert.Equal(created.Id, persisted.BusinessId);
        Assert.NotNull(persisted.ConsumedAt);
        Assert.Equal(2, persisted.Sources.Count);
        Assert.NotEmpty(persisted.Evidence);
        Assert.Contains(await db.AuditRecords.Where(x => x.BusinessId == created.Id).ToListAsync(),
            x => x.Action == "business.discovery.confirmed");
    }

    private static PublicBusinessFact Fact(string key, string value, string provider, string url, DateTimeOffset observedAt) =>
        new(key, value, provider, url, observedAt, "high");
}
