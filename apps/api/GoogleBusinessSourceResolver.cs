using System.Net;
using System.Text.RegularExpressions;

namespace Atlas.Api;

public sealed record ResolvedGoogleBusinessSource(
    string CanonicalSourceUrl,
    string Query,
    string? PlaceId);

public static class GoogleBusinessSourceHttpHandlerFactory
{
    public static SocketsHttpHandler Create() => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
        ConnectCallback = PublicBusinessHttpConnector.ConnectAsync,
    };
}

public sealed partial class GoogleBusinessSourceResolver(HttpClient client)
{
    public const int MaxRedirects = 4;

    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "maps.app.goo.gl",
        "maps.google.com",
        "google.com",
        "www.google.com",
    };

    public async Task<ResolvedGoogleBusinessSource> ResolveAsync(CanonicalBusinessUrl source, CancellationToken ct)
    {
        if (source.Kind != BusinessSourceKind.GoogleMaps)
            throw new BusinessDiscoveryException("business_google_source_invalid", "That source is not a Google Maps business location.");

        var originalSourceUrl = source.Value;
        var current = source.Uri;

        if (!current.IdnHost.Equals("maps.app.goo.gl", StringComparison.OrdinalIgnoreCase) &&
            TryResolveSpecificPlace(current, originalSourceUrl, out var direct))
            return direct!;

        var redirects = 0;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            ValidateGoogleRequestTarget(current);
            if (!visited.Add(current.AbsoluteUri))
                throw new BusinessDiscoveryException("business_google_redirect_invalid", "That Google Maps link is stuck in a redirect loop.");

            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.UserAgent.ParseAdd("AtlasBusinessDiscovery/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            if (IsRedirect(response.StatusCode))
            {
                if (redirects >= MaxRedirects)
                    throw new BusinessDiscoveryException("business_google_redirect_limit", "That Google Maps link redirected too many times. Share the final business location link instead.");

                var location = response.Headers.Location;
                if (location is null)
                    throw new BusinessDiscoveryException("business_google_redirect_invalid", "That Google Maps link returned an invalid redirect.");

                Uri candidate;
                try
                {
                    candidate = location.IsAbsoluteUri ? location : new Uri(current, location);
                }
                catch (UriFormatException)
                {
                    throw new BusinessDiscoveryException("business_google_redirect_invalid", "That Google Maps link returned an invalid redirect.");
                }

                ValidateGoogleRequestTarget(candidate);
                current = candidate;
                redirects++;
                continue;
            }

            if (!response.IsSuccessStatusCode)
                throw new BusinessDiscoveryException("business_google_source_unavailable", "Atlas could not resolve that Google Maps business link right now.");

            if (TryResolveSpecificPlace(current, originalSourceUrl, out var resolved))
                return resolved!;

            throw new BusinessDiscoveryException("business_google_place_unresolved", "That Google Maps link does not identify one business location. Share the business profile/location instead.");
        }
    }

    private static void ValidateGoogleRequestTarget(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps ||
            uri.Port != 443 ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !AllowedHosts.Contains(uri.IdnHost.TrimEnd('.')) ||
            !PublicBusinessUrlPolicy.TryValidate(uri.AbsoluteUri, out _, out _))
            throw new BusinessDiscoveryException("business_google_redirect_invalid", "That Google Maps link redirected outside Atlas's approved Google Maps boundary.");
    }

    private static bool TryResolveSpecificPlace(
        Uri uri,
        string originalSourceUrl,
        out ResolvedGoogleBusinessSource? resolved)
    {
        resolved = null;
        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (!segments[index].Equals("place", StringComparison.OrdinalIgnoreCase)) continue;
            var query = HumanizePathSegment(segments[index + 1]);
            if (query.Length == 0) return false;
            resolved = new ResolvedGoogleBusinessSource(originalSourceUrl, query, StrongIdentifier(uri.Query));
            return true;
        }

        var placeId = StrongIdentifier(uri.Query);
        if (!string.IsNullOrWhiteSpace(placeId))
        {
            var query = QueryValue(uri.Query, "query") ?? QueryValue(uri.Query, "q") ?? placeId;
            resolved = new ResolvedGoogleBusinessSource(originalSourceUrl, query.Trim(), placeId);
            return true;
        }

        return false;
    }

    private static string HumanizePathSegment(string value)
    {
        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(value);
        }
        catch (UriFormatException)
        {
            decoded = value;
        }

        decoded = decoded.Replace('+', ' ');
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    private static string? StrongIdentifier(string query) =>
        QueryValue(query, "query_place_id") ?? QueryValue(query, "cid") ?? QueryValue(query, "ftid");

    private static string? QueryValue(string query, string requestedKey)
    {
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            var rawKey = separator < 0 ? part : part[..separator];
            var rawValue = separator < 0 ? string.Empty : part[(separator + 1)..];
            var key = Decode(rawKey);
            if (!key.Equals(requestedKey, StringComparison.OrdinalIgnoreCase)) continue;
            var value = Decode(rawValue);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }

    private static string Decode(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value.Replace('+', ' ')).Trim();
        }
        catch (UriFormatException)
        {
            return value.Trim();
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MovedPermanently or
        HttpStatusCode.Redirect or
        HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or
        HttpStatusCode.PermanentRedirect;

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
