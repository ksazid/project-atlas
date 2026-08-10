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
}
