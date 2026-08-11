using System.Text.Json;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryRequestContractTests
{
    [Fact]
    public void Request_DeserializesPrimaryAndOptionalAdditionalUrlsInOrder()
    {
        const string json = """
            {
              "url": "https://restaurant.example/menu",
              "additionalUrls": [
                "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians",
                "https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86"
              ]
            }
            """;

        var request = JsonSerializer.Deserialize<DiscoverBusinessRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.Equal("https://restaurant.example/menu", request!.Url);
        Assert.Equal(2, request.AdditionalUrls?.Count);
        Assert.Equal("https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians", request.AdditionalUrls?[0]);
        Assert.Equal("https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86", request.AdditionalUrls?[1]);
    }
}
