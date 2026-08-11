using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class LiveBoltBusinessDiscoverySmokeTests
{
    private const string PublicBoltUrl = "https://food.bolt.eu/en/324/p/11881-gun-turkish-kebab?utm_source=share_provider&utm_medium=product&utm_content=menu_header";

    [Fact]
    public async Task Supplied_public_bolt_business_page_is_readable_by_production_discovery_code()
    {
        Assert.True(BusinessSourceUrlPolicy.TryCanonicalize(PublicBoltUrl, out var canonical, out var error), error);
        Assert.NotNull(canonical);
        Assert.Equal(BusinessSourceKind.BoltFood, canonical!.Kind);
        Assert.DoesNotContain("utm_", canonical.Value, StringComparison.OrdinalIgnoreCase);

        using var handler = PublicBusinessHttpHandlerFactory.Create();
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        var service = new BusinessDiscoveryService(client);

        var snapshot = await service.DiscoverAsync(canonical.Value, CancellationToken.None);

        Assert.Equal("bolt-food", snapshot.Provider);
        Assert.Contains(snapshot.Facts, fact => fact.Key == "category" && fact.Value == "restaurant-cafe");
        var name = Assert.Single(snapshot.Facts, fact => fact.Key == "name").Value;
        Assert.False(string.Equals(name, "Bolt Food", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("kebab", name, StringComparison.OrdinalIgnoreCase);
    }
}
