using System.Net;
using System.Text;
using Atlas.Api;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class MultiSourceBusinessDiscoveryServiceTests
{
    [Fact]
    public async Task Discovery_ValidatesEverySourceBeforeAnyNetworkRequest()
    {
        var pageHandler = new RoutedHandler(_ => Html("<title>Should not be requested</title>"));
        var service = Service(pageHandler);

        var error = await Assert.ThrowsAsync<BusinessDiscoveryException>(() => service.DiscoverAsync(
            "https://restaurant.example/menu",
            ["https://127.0.0.1/private"],
            CancellationToken.None));

        Assert.Equal("business_url_invalid", error.Code);
        Assert.Empty(pageHandler.Requests);
    }

    [Fact]
    public async Task Discovery_UsesOwnerPriority_AndSecondaryFillsOnlyMissingFacts()
    {
        var pageHandler = new RoutedHandler(request => request.RequestUri!.Host switch
        {
            "restaurant.example" => Html("""
                <html><head>
                  <meta property="og:title" content="Antalya Kebab St. Julian's" />
                  <script type="application/ld+json">
                  {"@context":"https://schema.org","@type":"Restaurant","name":"Antalya Kebab St. Julian's","telephone":"+356 2100 0000"}
                  </script>
                </head></html>
                """),
            "food.bolt.eu" => Html("""
                <html><head>
                  <meta property="og:title" content="Antalya Kebab St. Julian's - Bolt Food" />
                  <meta property="og:description" content="Turkish kebab restaurant" />
                </head></html>
                """),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var service = Service(pageHandler);

        var result = await service.DiscoverAsync(
            "https://restaurant.example/menu?utm_source=share",
            ["https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians?utm_medium=product"],
            CancellationToken.None);

        Assert.Equal("Antalya Kebab St. Julian's", result.Snapshot.Facts.Single(x => x.Key == "name").Value);
        Assert.Equal("+356 2100 0000", result.Snapshot.Facts.Single(x => x.Key == "phone").Value);
        Assert.Equal("Turkish kebab restaurant", result.Snapshot.Facts.Single(x => x.Key == "description").Value);
        Assert.Equal(2, pageHandler.Requests.Count);
        Assert.Equal("https://restaurant.example/menu", pageHandler.Requests[0].AbsoluteUri);
        Assert.Equal("https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians", pageHandler.Requests[1].AbsoluteUri);
        Assert.Contains(result.Evidence, x => x.SourceOrder == 0 && x.Key == "name" && x.ReconciliationState == "selected");
        Assert.Contains(result.Evidence, x => x.SourceOrder == 1 && x.Key == "name" && x.ReconciliationState == "corroborating");
    }

    [Fact]
    public async Task Discovery_OptionalSourceFailureDegradesWhenPrimaryIsUseful()
    {
        var pageHandler = new RoutedHandler(request => request.RequestUri!.Host switch
        {
            "restaurant.example" => Html("<html><head><meta property=\"og:title\" content=\"Antalya Kebab St. Julian's\" /></head></html>"),
            "food.bolt.eu" => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var service = Service(pageHandler);

        var result = await service.DiscoverAsync(
            "https://restaurant.example/menu",
            ["https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians"],
            CancellationToken.None);

        Assert.Equal("Antalya Kebab St. Julian's", result.Snapshot.Facts.Single(x => x.Key == "name").Value);
        Assert.Contains(result.SourceResults, x => x.Order == 1 && x.Status == "unavailable");
        Assert.Contains("business_source_unavailable", result.Warnings);
    }

    [Fact]
    public async Task Discovery_GooglePlaceSourceUsesPlacesProvider_NotGoogleMapsHtml()
    {
        var pageHandler = new RoutedHandler(request => request.RequestUri!.Host switch
        {
            "restaurant.example" => Html("<html><head><meta property=\"og:title\" content=\"Antalya Kebab St. Julian's\" /></head></html>"),
            _ => throw new InvalidOperationException($"Unexpected page fetch: {request.RequestUri}"),
        });
        var placeProvider = new RecordingLocationProvider([
            new BusinessLocationCandidate(
                "places/antalya",
                "Antalya Kebab St. Julian's",
                "St George's Road, St Julian's, Malta",
                35.918,
                14.489,
                "MT",
                "Malta",
                "Europe/Malta",
                "EUR",
                "google-places")
            {
                BusinessTypeSummary = "Turkish · Kebab"
            }
        ]);
        var service = Service(pageHandler, placeProvider);

        var result = await service.DiscoverAsync(
            "https://restaurant.example/menu",
            ["https://www.google.com/maps/place/Antalya+Kebab+St.+Julian%27s/@35.918,14.489,17z"],
            CancellationToken.None);

        Assert.Single(pageHandler.Requests);
        Assert.Single(placeProvider.Queries);
        Assert.Equal("Antalya Kebab St. Julian's", placeProvider.Queries[0]);
        Assert.Equal("St George's Road, St Julian's, Malta", result.Snapshot.Facts.Single(x => x.Key == "primaryLocation").Value);
        Assert.Equal("MT", result.Snapshot.Facts.Single(x => x.Key == "country").Value);
        Assert.Equal("Europe/Malta", result.Snapshot.Facts.Single(x => x.Key == "timezone").Value);
        Assert.Equal("EUR", result.Snapshot.Facts.Single(x => x.Key == "currency").Value);
    }

    private static MultiSourceBusinessDiscoveryService Service(
        RoutedHandler pageHandler,
        IBusinessLocationProvider? locationProvider = null)
    {
        var pageClient = new HttpClient(pageHandler);
        var pageDiscovery = new BusinessDiscoveryService(pageClient);
        var googleResolver = new GoogleBusinessSourceResolver(new HttpClient(new RoutedHandler(
            _ => throw new InvalidOperationException("Direct Google place URLs must not require Maps HTML."))));
        return new MultiSourceBusinessDiscoveryService(
            pageDiscovery,
            googleResolver,
            locationProvider ?? new RecordingLocationProvider([]));
    }

    private static HttpResponseMessage Html(string value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(value, Encoding.UTF8, "text/html")
    };

    private sealed class RoutedHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri ?? throw new InvalidOperationException("Request URI is required."));
            return Task.FromResult(responder(request));
        }
    }

    private sealed class RecordingLocationProvider(IReadOnlyList<BusinessLocationCandidate> candidates) : IBusinessLocationProvider
    {
        public bool IsConfigured => true;
        public List<string> Queries { get; } = [];

        public Task<IReadOnlyList<BusinessLocationCandidate>> SearchAsync(string query, CancellationToken ct)
        {
            Queries.Add(query);
            return Task.FromResult(candidates);
        }
    }
}
