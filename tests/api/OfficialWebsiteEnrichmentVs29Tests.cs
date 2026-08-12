using System.Net;
using System.Text;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OfficialWebsiteEnrichmentVs29Tests
{
    [Fact]
    public async Task Discovery_fetches_one_strongly_matched_official_site_as_secondary_observation()
    {
        var handler = new RoutedHandler(request => request.RequestUri!.Host switch
        {
            "directory.example" => Html("""
                <script type="application/ld+json">
                {
                  "@context":"https://schema.org",
                  "@type":"Restaurant",
                  "name":"Harbour Coffee",
                  "url":"https://harbour.example/",
                  "telephone":"+35621000000"
                }
                </script>
                """),
            "harbour.example" => Html("""
                <script type="application/ld+json">
                {
                  "@context":"https://schema.org",
                  "@type":"Restaurant",
                  "name":"Harbour Coffee",
                  "url":"https://harbour.example/",
                  "telephone":"+35621000000",
                  "email":"hello@harbour.example",
                  "sameAs":["https://instagram.com/harbourcoffee/"]
                }
                </script>
                """),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var service = Service(handler);

        var result = await service.DiscoverAsync(
            "https://directory.example/listing/harbour-coffee",
            null,
            CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("directory.example", handler.Requests[0].Host);
        Assert.Equal("harbour.example", handler.Requests[1].Host);
        Assert.Equal("hello@harbour.example", result.Snapshot.Facts.Single(x => x.Key == "email").Value);
        Assert.Contains(result.SourceResults, source =>
            !source.IsPrimary &&
            source.Provider == "website" &&
            source.CanonicalUrl.StartsWith("https://harbour.example", StringComparison.Ordinal));
        Assert.Contains(result.Evidence, evidence =>
            evidence.Key == "email" &&
            evidence.Provider == "website" &&
            evidence.SourceOrder > 0);
    }

    [Fact]
    public async Task Discovery_discards_automatic_official_site_when_identity_is_not_strongly_matched()
    {
        var handler = new RoutedHandler(request => request.RequestUri!.Host switch
        {
            "directory.example" => Html("""
                <script type="application/ld+json">
                {
                  "@context":"https://schema.org",
                  "@type":"Restaurant",
                  "name":"Harbour Coffee",
                  "url":"https://other.example/",
                  "telephone":"+35621000000"
                }
                </script>
                """),
            "other.example" => Html("""
                <script type="application/ld+json">
                {
                  "@context":"https://schema.org",
                  "@type":"Restaurant",
                  "name":"Different Business",
                  "url":"https://other.example/",
                  "telephone":"+35621000000",
                  "email":"wrong@other.example"
                }
                </script>
                """),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var service = Service(handler);

        var result = await service.DiscoverAsync(
            "https://directory.example/listing/harbour-coffee",
            null,
            CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.DoesNotContain(result.Snapshot.Facts, x => x.Key == "email");
        Assert.Single(result.SourceResults);
    }

    [Fact]
    public async Task Discovery_without_official_website_fact_makes_no_extra_request()
    {
        var handler = new RoutedHandler(_ => Html("<meta property=\"og:title\" content=\"Harbour Coffee\" />"));
        var service = Service(handler);

        _ = await service.DiscoverAsync("https://directory.example/listing/harbour-coffee", null, CancellationToken.None);

        Assert.Single(handler.Requests);
    }

    private static MultiSourceBusinessDiscoveryService Service(RoutedHandler handler)
    {
        var pageDiscovery = new BusinessDiscoveryService(new HttpClient(handler));
        var googleResolver = new GoogleBusinessSourceResolver(new HttpClient(new RoutedHandler(
            _ => throw new InvalidOperationException("Google source should not be used by this regression."))));
        return new MultiSourceBusinessDiscoveryService(
            pageDiscovery,
            googleResolver,
            new EmptyLocationProvider());
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

    private sealed class EmptyLocationProvider : IBusinessLocationProvider
    {
        public bool IsConfigured => true;
        public Task<IReadOnlyList<BusinessLocationCandidate>> SearchAsync(string query, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<BusinessLocationCandidate>>([]);
    }
}
