using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class LiveHasanMediaMenuSmokeTests
{
    private const string PublicBoltUrl = "https://food.bolt.eu/en/324-valletta/p/1310-hasans-turkish-kebab-house/";

    [Fact]
    public async Task Hasan_public_bolt_page_exposes_media_and_structured_menu_through_production_discovery_path()
    {
        Assert.True(BusinessSourceUrlPolicy.TryCanonicalize(PublicBoltUrl, out var canonical, out var error), error);
        Assert.NotNull(canonical);
        Assert.Equal(BusinessSourceKind.BoltFood, canonical!.Kind);

        using var handler = PublicBusinessHttpHandlerFactory.Create();
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        var service = new BusinessDiscoveryService(client);
        var snapshot = await service.DiscoverAsync(canonical.Value, CancellationToken.None);

        Assert.Equal("bolt-food", snapshot.Provider);
        Assert.Contains(snapshot.Facts, fact =>
            fact.Key == "name" &&
            fact.Value.Contains("Hasan", StringComparison.OrdinalIgnoreCase));

        Assert.NotEmpty(snapshot.Media);
        Assert.All(snapshot.Media, item =>
        {
            Assert.StartsWith("https://", item.RemoteUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("bolt-food", item.Source);
            Assert.False(item.OwnerConfirmed);
        });

        Assert.True(snapshot.Offerings.Count >= 5, $"Expected a useful public menu, found {snapshot.Offerings.Count} offering(s).");
        Assert.Contains(snapshot.Offerings, item =>
            item.Section == "Beverages" &&
            item.Name == "Ice Tea Peach" &&
            item.Price == 2.50m &&
            item.Currency == "EUR");
        Assert.Contains(snapshot.Offerings, item =>
            item.Section == "Wraps & Pita" &&
            item.Name == "Any Grill in Pita Bread" &&
            item.Price == 9.50m &&
            item.Currency == "EUR");

        Assert.All(snapshot.Offerings, item =>
        {
            Assert.Equal("menu-item", item.Kind);
            Assert.Equal("bolt-food", item.Source);
            Assert.False(item.OwnerConfirmed);
        });
    }
}