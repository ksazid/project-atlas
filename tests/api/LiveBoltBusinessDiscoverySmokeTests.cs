using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class LiveBoltBusinessDiscoverySmokeTests
{
    private const string PublicBoltUrl = "https://food.bolt.eu/en/324/p/11881-gun-turkish-kebab?utm_source=share_provider&utm_medium=product&utm_content=menu_header";

    [Fact]
    public async Task Supplied_public_bolt_business_page_is_readable_by_production_discovery_code()
    {
        using var handler = PublicBusinessHttpHandlerFactory.Create();
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        using var request = new HttpRequestMessage(HttpMethod.Get, PublicBoltUrl);
        request.Headers.UserAgent.ParseAdd("AtlasBusinessDiscovery/1.0");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);

        Assert.True(
            response.IsSuccessStatusCode,
            $"Live Bolt terminal response was HTTP {(int)response.StatusCode} ({response.StatusCode}); Location={response.Headers.Location?.ToString() ?? "<none>"}; ETag={response.Headers.ETag?.ToString() ?? "<none>"}.");

        var service = new BusinessDiscoveryService(client);
        var snapshot = await service.DiscoverAsync(PublicBoltUrl, CancellationToken.None);

        Assert.Equal("bolt-food", snapshot.Provider);
        Assert.Contains(snapshot.Facts, fact => fact.Key == "category" && fact.Value == "restaurant-cafe");
        var name = Assert.Single(snapshot.Facts, fact => fact.Key == "name").Value;
        Assert.False(string.Equals(name, "Bolt Food", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("kebab", name, StringComparison.OrdinalIgnoreCase);
    }
}
