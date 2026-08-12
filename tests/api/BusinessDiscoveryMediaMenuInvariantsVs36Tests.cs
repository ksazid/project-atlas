using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryMediaMenuInvariantsVs36Tests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Mismatched_secondary_media_and_offerings_never_enter_the_selected_snapshot()
    {
        var result = BusinessDiscoveryReconciler.Reconcile([
            new BusinessSourceObservation(
                0,
                true,
                "website",
                "https://antalya.example/",
                "success",
                Facts("Antalya Kebab")),
            new BusinessSourceObservation(
                1,
                false,
                "website",
                "https://unrelated.example/",
                "success",
                Facts("Completely Different Florist"),
                Media:
                [
                    new PublicBusinessMedia(
                        "business-image",
                        "https://cdn.unrelated.example/hero.jpg",
                        "website",
                        "https://unrelated.example/",
                        ObservedAt,
                        "high")
                ],
                Offerings:
                [
                    new PublicBusinessOffering(
                        "menu-item",
                        "Flowers",
                        "Rose bouquet",
                        null,
                        25m,
                        "EUR",
                        "website",
                        "https://unrelated.example/",
                        ObservedAt,
                        "high")
                ])
        ]);

        Assert.Empty(result.Snapshot.Media);
        Assert.Empty(result.Snapshot.Offerings);
        Assert.Contains(result.SourceResults, source => source.Order == 1 && source.AssociationStatus == "mismatch");
        Assert.Contains("business_source_identity_mismatch", result.Warnings);
    }

    [Fact]
    public async Task Renderer_required_warning_persists_without_materialising_false_menu_or_media()
    {
        await using var db = Db();
        const string subject = "vs36-renderer-owner";
        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            ProviderSubject = subject,
            CreatedAt = ObservedAt
        };
        var sourceUrl = "https://food.bolt.eu/en/324-valletta/p/1310-antalya-kebab/";
        var reconciliation = BusinessDiscoveryReconciler.Reconcile([
            new BusinessSourceObservation(
                0,
                true,
                "bolt-food",
                sourceUrl,
                "success",
                [
                    new PublicBusinessFact("name", "Antalya Kebab", "bolt-food", sourceUrl, ObservedAt, "high"),
                    new PublicBusinessFact("category", "restaurant-cafe", "bolt-food", sourceUrl, ObservedAt, "high"),
                    new PublicBusinessFact("primaryLocation", "Birkirkara, Malta", "bolt-food", sourceUrl, ObservedAt, "high")
                ],
                WarningCode: "business_source_menu_renderer_required")
        ]);
        var snapshot = BusinessDiscoverySnapshot.Create(account.Id, reconciliation);

        db.UserAccounts.Add(account);
        db.BusinessDiscoverySnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var storedSource = Assert.Single(snapshot.Sources);
        Assert.Equal("business_source_menu_renderer_required", storedSource.WarningCode);
        Assert.Empty(snapshot.Media);
        Assert.Empty(snapshot.Offerings);
        Assert.DoesNotContain(snapshot.Facts, fact => fact.Key.Contains("coverage", StringComparison.OrdinalIgnoreCase));

        var business = await BusinessDiscoveryBusinessCreator.CreateAsync(
            db,
            subject,
            new CreateBusinessFromDiscoveryRequest(
                snapshot.Id,
                "Antalya Kebab",
                "restaurant-cafe",
                "restaurant",
                "MT",
                "Europe/Malta",
                "EUR",
                "Birkirkara, Malta",
                "Open",
                "Turkish restaurant",
                null,
                null,
                null,
                "en",
                true),
            CancellationToken.None);

        Assert.Empty(await db.Set<BusinessMediaReference>().Where(item => item.BusinessId == business.Id).ToListAsync());
        Assert.Empty(await db.Set<BusinessOffering>().Where(item => item.BusinessId == business.Id).ToListAsync());
    }

    private static IReadOnlyList<PublicBusinessFact> Facts(string name) =>
    [
        new PublicBusinessFact("name", name, "website", "https://source.example/", ObservedAt, "high")
    ];

    private static AtlasDbContext Db() => new(
        new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}