using System.Net;
using System.Text;
using Atlas.Api;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessPlaceEnrichmentTests
{
    [Fact]
    public async Task Google_provider_uses_exact_place_id_and_minimal_explicit_field_mask()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://places.googleapis.com/v1/places/ChIJAtlas123", request.RequestUri?.ToString());
            Assert.True(request.Headers.TryGetValues("X-Goog-FieldMask", out var masks));
            Assert.Equal(
                "id,dineIn,takeout,delivery,reservable,servesBreakfast,servesBrunch,servesLunch,servesDinner,priceLevel,regularOpeningHours,attributions",
                Assert.Single(masks));

            const string body = """
            {
              "id": "ChIJAtlas123",
              "dineIn": true,
              "takeout": true,
              "delivery": true,
              "reservable": true,
              "servesLunch": true,
              "servesDinner": true,
              "priceLevel": "PRICE_LEVEL_MODERATE",
              "regularOpeningHours": {
                "weekdayDescriptions": ["Monday: 11:00 AM – 10:00 PM", "Tuesday: 11:00 AM – 10:00 PM"]
              },
              "attributions": [
                { "provider": "Example data provider", "providerUri": "https://example.com/provider" }
              ]
            }
            """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });
        using var client = new HttpClient(handler);
        var provider = new GoogleBusinessPlaceEnrichmentProvider(client, Configuration("test-key"));

        var result = await provider.GetAsync("ChIJAtlas123", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("ChIJAtlas123", result.ProviderRef);
        Assert.Equal(["Dine in", "Takeaway", "Delivery"], result.OperatingChannels);
        Assert.True(result.Reservable);
        Assert.Equal(["Lunch", "Dinner"], result.ServicePeriods);
        Assert.Equal("Moderate", result.PricePosition);
        Assert.Equal(2, result.OpeningHours.Count);
        Assert.Equal("Example data provider", Assert.Single(result.Attributions).Provider);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task Google_provider_maps_absent_boolean_fields_as_unknown_not_false()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
              "id": "ChIJAtlas123",
              "dineIn": true,
              "takeout": false,
              "servesLunch": true
            }
            """, Encoding.UTF8, "application/json")
        });
        using var client = new HttpClient(handler);
        var provider = new GoogleBusinessPlaceEnrichmentProvider(client, Configuration("test-key"));

        var result = await provider.GetAsync("ChIJAtlas123", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Reservable);
        Assert.Equal(["Dine in"], result.OperatingChannels);
        Assert.DoesNotContain("Delivery", result.OperatingChannels);
        Assert.Equal(["Lunch"], result.ServicePeriods);
    }

    [Fact]
    public async Task Google_provider_rejects_wildcard_behavior_by_emitting_only_the_approved_mask()
    {
        string? observedMask = null;
        var handler = new RecordingHandler(request =>
        {
            observedMask = request.Headers.GetValues("X-Goog-FieldMask").Single();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"ChIJAtlas123\"}", Encoding.UTF8, "application/json")
            };
        });
        using var client = new HttpClient(handler);
        var provider = new GoogleBusinessPlaceEnrichmentProvider(client, Configuration("test-key"));

        _ = await provider.GetAsync("ChIJAtlas123", CancellationToken.None);

        Assert.NotNull(observedMask);
        Assert.DoesNotContain("*", observedMask, StringComparison.Ordinal);
        Assert.DoesNotContain("reviews", observedMask, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("photos", observedMask, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rating", observedMask, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Google_provider_degrades_on_http_failure_without_exposing_the_api_key()
    {
        const string apiKey = "super-secret-test-key";
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        using var client = new HttpClient(handler);
        var provider = new GoogleBusinessPlaceEnrichmentProvider(client, Configuration(apiKey));

        var exception = await Record.ExceptionAsync(() => provider.GetAsync("ChIJAtlas123", CancellationToken.None));

        Assert.Null(exception);
        var result = await provider.GetAsync("ChIJAtlas123", CancellationToken.None);
        Assert.Null(result);
        Assert.Equal(2, handler.RequestCount);
    }

    private static IConfiguration Configuration(string? apiKey) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["GoogleMaps:ApiKey"] = apiKey })
        .Build();

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responder(request));
        }
    }
}
