using System.Net;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryRedirectTests
{
    [Fact]
    public async Task Handler_FollowsRelativeHttpsRedirect_AndReturnsFinalResponse()
    {
        var inner = new SequenceHandler(
            request => Redirect(HttpStatusCode.Found, new Uri("/en/324/p/87179-aleppo-food", UriKind.Relative)),
            request => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><head><title>Aleppo Food | Bolt Food</title></head></html>")
            });
        using var handler = new PublicBusinessRedirectHandler(inner);
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://food.bolt.eu/en/324/p/87179-aleppo-food-old");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Requests.Count);
        Assert.Equal("https://food.bolt.eu/en/324/p/87179-aleppo-food", inner.Requests[1].AbsoluteUri);
    }

    [Theory]
    [InlineData("http://example.com/final")]
    [InlineData("https://127.0.0.1/private")]
    [InlineData("https://10.0.0.8/private")]
    [InlineData("https://example.com:8443/private")]
    [InlineData("https://user:pass@example.com/private")]
    public async Task Handler_RejectsUnsafeRedirectTargets_BeforeSecondRequest(string target)
    {
        var inner = new SequenceHandler(request => Redirect(HttpStatusCode.Found, new Uri(target)));
        using var handler = new PublicBusinessRedirectHandler(inner);
        using var client = new HttpClient(handler);

        var error = await Assert.ThrowsAsync<BusinessDiscoveryException>(() =>
            client.GetAsync("https://food.bolt.eu/en/324/p/87179-aleppo-food"));

        Assert.Equal("business_source_redirect_unsafe", error.Code);
        Assert.Single(inner.Requests);
    }

    [Fact]
    public async Task Handler_RejectsRedirectLoops()
    {
        var inner = new SequenceHandler(request => Redirect(HttpStatusCode.Found, new Uri("/loop", UriKind.Relative)));
        using var handler = new PublicBusinessRedirectHandler(inner);
        using var client = new HttpClient(handler);

        var error = await Assert.ThrowsAsync<BusinessDiscoveryException>(() =>
            client.GetAsync("https://food.bolt.eu/loop"));

        Assert.Equal("business_source_redirect_loop", error.Code);
        Assert.Single(inner.Requests);
    }

    [Theory]
    [InlineData("https://share.google/guJAzxecjEv9AE195")]
    [InlineData("https://maps.app.goo.gl/ExampleShortLink")]
    public async Task Handler_RejectsGoogleMapsShortLinks_BeforeAnyNetworkRequest(string url)
    {
        var inner = new SequenceHandler();
        using var handler = new PublicBusinessRedirectHandler(inner);
        using var client = new HttpClient(handler);

        var error = await Assert.ThrowsAsync<BusinessDiscoveryException>(() => client.GetAsync(url));

        Assert.Equal("business_google_maps_short_link", error.Code);
        Assert.Contains("search the business name or address", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(inner.Requests);
    }

    [Fact]
    public void GoogleHandler_DisablesAutomaticRedirectsAndProxyRouting()
    {
        using var handler = GoogleBusinessSourceHttpHandlerFactory.Create();

        Assert.False(handler.AllowAutoRedirect);
        Assert.False(handler.UseProxy);
        Assert.NotNull(handler.ConnectCallback);
    }

    [Fact]
    public async Task GoogleResolver_FollowsOnlyGoogleRedirects_AndExtractsSpecificPlaceQuery()
    {
        var inner = new SequenceHandler(
            _ => Redirect(HttpStatusCode.Found, new Uri("https://www.google.com/maps/place/Antalya+Kebab+St.+Julian%27s/@35.918,14.489,17z")),
            _ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(inner);
        Assert.True(BusinessSourceUrlPolicy.TryCanonicalize(
            "https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86?g_st=ic",
            out var source,
            out var error), error);
        var resolver = new GoogleBusinessSourceResolver(client);

        var resolved = await resolver.ResolveAsync(source!, CancellationToken.None);

        Assert.Equal(2, inner.Requests.Count);
        Assert.Equal("Antalya Kebab St. Julian's", resolved.Query);
        Assert.Equal("https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86", resolved.CanonicalSourceUrl);
    }

    [Fact]
    public async Task GoogleResolver_RejectsCrossProviderRedirectBeforeRequestingTarget()
    {
        var inner = new SequenceHandler(
            _ => Redirect(HttpStatusCode.Found, new Uri("https://evil.example/steal")));
        using var client = new HttpClient(inner);
        Assert.True(BusinessSourceUrlPolicy.TryCanonicalize(
            "https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86",
            out var source,
            out _));
        var resolver = new GoogleBusinessSourceResolver(client);

        var error = await Assert.ThrowsAsync<BusinessDiscoveryException>(() =>
            resolver.ResolveAsync(source!, CancellationToken.None));

        Assert.Equal("business_google_redirect_invalid", error.Code);
        Assert.Single(inner.Requests);
    }

    [Fact]
    public async Task GoogleResolver_CapsRedirectChainAtFourHops()
    {
        var inner = new RepeatingRedirectHandler();
        using var client = new HttpClient(inner);
        Assert.True(BusinessSourceUrlPolicy.TryCanonicalize(
            "https://maps.app.goo.gl/0000",
            out var source,
            out _));
        var resolver = new GoogleBusinessSourceResolver(client);

        var error = await Assert.ThrowsAsync<BusinessDiscoveryException>(() =>
            resolver.ResolveAsync(source!, CancellationToken.None));

        Assert.Equal("business_google_redirect_limit", error.Code);
        Assert.Equal(5, inner.Requests.Count);
    }

    [Fact]
    public async Task GoogleResolver_RejectsFinalGoogleUrlThatDoesNotIdentifyOneBusiness()
    {
        var inner = new SequenceHandler(
            _ => Redirect(HttpStatusCode.Found, new Uri("https://www.google.com/maps/search/restaurants+malta")),
            _ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(inner);
        Assert.True(BusinessSourceUrlPolicy.TryCanonicalize(
            "https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86",
            out var source,
            out _));
        var resolver = new GoogleBusinessSourceResolver(client);

        var error = await Assert.ThrowsAsync<BusinessDiscoveryException>(() =>
            resolver.ResolveAsync(source!, CancellationToken.None));

        Assert.Equal("business_google_place_unresolved", error.Code);
    }

    private static HttpResponseMessage Redirect(HttpStatusCode statusCode, Uri location)
    {
        var response = new HttpResponseMessage(statusCode);
        response.Headers.Location = location;
        return response;
    }

    private sealed class SequenceHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> queue = new(responses);
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri ?? throw new InvalidOperationException("Request URI is required."));
            if (queue.Count == 0) throw new InvalidOperationException("No fake response is configured.");
            return Task.FromResult(queue.Dequeue()(request));
        }
    }

    private sealed class RepeatingRedirectHandler : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri ?? throw new InvalidOperationException("Request URI is required.");
            Requests.Add(uri);
            var current = int.TryParse(uri.AbsolutePath.Trim('/'), out var value) ? value : 0;
            return Task.FromResult(Redirect(HttpStatusCode.Found, new Uri($"https://maps.app.goo.gl/{current + 1:D4}")));
        }
    }
}
