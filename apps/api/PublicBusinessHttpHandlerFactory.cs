using System.Net;

namespace Atlas.Api;

public static class PublicBusinessHttpHandlerFactory
{
    public static PublicBusinessRedirectHandler Create() => new();
}

public sealed class PublicBusinessRedirectHandler : DelegatingHandler
{
    public const int MaxRedirects = 5;
    private readonly SocketsHttpHandler? transport;

    public PublicBusinessRedirectHandler() : this(CreateTransport())
    {
    }

    public PublicBusinessRedirectHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    private PublicBusinessRedirectHandler(SocketsHttpHandler transport) : base(transport)
    {
        this.transport = transport;
    }

    public bool AllowAutoRedirect => transport?.AllowAutoRedirect ?? false;
    public bool UseProxy => transport?.UseProxy ?? false;
    public Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>>? ConnectCallback => transport?.ConnectCallback;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is null ||
            !PublicBusinessUrlPolicy.TryValidate(request.RequestUri.AbsoluteUri, out var currentUri, out _) ||
            currentUri is null)
            throw new BusinessDiscoveryException("business_url_invalid", "Use a valid HTTPS business page URL.");

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { currentUri.AbsoluteUri };
        var redirects = 0;

        while (true)
        {
            using var outbound = CloneRequest(request, currentUri);
            var response = await base.SendAsync(outbound, cancellationToken).ConfigureAwait(false);
            if (!IsRedirect(response.StatusCode)) return response;

            if (redirects >= MaxRedirects)
            {
                response.Dispose();
                throw new BusinessDiscoveryException("business_source_redirect_limit", "That business page redirected too many times. Use a direct public business URL or set up manually.");
            }

            var location = response.Headers.Location;
            response.Dispose();
            if (location is null)
                throw new BusinessDiscoveryException("business_source_redirect_invalid", "That business page returned an invalid redirect. Use a direct public business URL or set up manually.");

            Uri candidate;
            try
            {
                candidate = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
            }
            catch (UriFormatException)
            {
                throw new BusinessDiscoveryException("business_source_redirect_invalid", "That business page returned an invalid redirect. Use a direct public business URL or set up manually.");
            }

            if (!PublicBusinessUrlPolicy.TryValidate(candidate.AbsoluteUri, out var validatedUri, out _) || validatedUri is null)
                throw new BusinessDiscoveryException("business_source_redirect_unsafe", "That business page redirected to a location Atlas cannot access safely. Use a direct public HTTPS business URL or set up manually.");

            if (!visited.Add(validatedUri.AbsoluteUri))
                throw new BusinessDiscoveryException("business_source_redirect_loop", "That business page is stuck in a redirect loop. Use a direct public business URL or set up manually.");

            currentUri = validatedUri;
            redirects++;
        }
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request, Uri requestUri)
    {
        var clone = new HttpRequestMessage(request.Method, requestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy,
        };
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MultipleChoices or
        HttpStatusCode.MovedPermanently or
        HttpStatusCode.Redirect or
        HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or
        HttpStatusCode.PermanentRedirect;

    private static SocketsHttpHandler CreateTransport() => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
        ConnectCallback = PublicBusinessHttpConnector.ConnectAsync,
    };
}
