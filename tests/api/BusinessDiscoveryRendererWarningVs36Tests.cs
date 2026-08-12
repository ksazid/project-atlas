using System.Net;
using System.Text;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryRendererWarningVs36Tests
{
    [Fact]
    public async Task Renderer_required_marketplace_source_stays_successful_and_carries_warning_provenance()
    {
        var handler = new RoutedHandler(_ => Html("""
            <html><head><title>Antalya Kebab | Bolt Food</title></head>
            <body><noscript>JavaScript is not enabled. Please enable JavaScript.</noscript></body></html>
            """));
        var pageDiscovery = new BusinessDiscoveryService(new HttpClient(handler));
        var service = new MultiSourceBusinessDiscoveryService(
            pageDiscovery,
            new GoogleBusinessSourceResolver(new HttpClient(new RoutedHandler(_ => throw new InvalidOperationException("Google is not used.")))),
            new EmptyLocationProvider());

        var result = await service.DiscoverAsync(
            "https://food.bolt.eu/en/324-valletta/p/1310-antalya-kebab/",
            null,
            CancellationToken.None);

        var source = Assert.Single(result.SourceResults);
        Assert.Equal("success", source.Status);
        Assert.Equal("anchor", source.AssociationStatus);
        Assert.Equal("business_source_menu_renderer_required", source.WarningCode);
        Assert.Contains("business_source_menu_renderer_required", result.Warnings);
        Assert.DoesNotContain(result.Snapshot.Facts, fact => fact.Key.Contains("coverage", StringComparison.OrdinalIgnoreCase));
    }

    private static HttpResponseMessage Html(string value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(value, Encoding.UTF8, "text/html")
    };

    private sealed class RoutedHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class EmptyLocationProvider : IBusinessLocationProvider
    {
        public bool IsConfigured => true;
        public Task<IReadOnlyList<BusinessLocationCandidate>> SearchAsync(string query, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<BusinessLocationCandidate>>([]);
    }
}