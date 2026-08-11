using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Atlas.Api;

public enum BusinessSourceKind
{
    Website,
    BoltFood,
    Wolt,
    GoogleMaps
}

public sealed record CanonicalBusinessUrl(Uri Uri, string Value, BusinessSourceKind Kind);

public static class BusinessSourceUrlPolicy
{
    public const int MaxSources = 3;
    private const int MaxRawInputCharacters = 8_192;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly Regex HttpsUrlRegex = new(
        @"https://[^\s<>""']+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeout);
    private static readonly string[] BlockedHostSuffixes = [".localhost", ".local", ".internal", ".home", ".lan", ".test"];
    private static readonly HashSet<string> TrackingKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "gclid", "dclid", "fbclid", "msclkid", "g_st", "mc_cid", "mc_eid",
        "ref", "referrer", "referral", "share", "share_id", "share_source", "source"
    };
    private static readonly HashSet<string> GoogleIdentityQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "query_place_id", "query", "q", "cid", "ftid"
    };

    public static bool TryCanonicalize(string? rawInput, out CanonicalBusinessUrl? canonical, out string? error)
    {
        canonical = null;
        error = null;

        if (!TryExtractSingleHttpsUrl(rawInput, out var candidate, out error) || candidate is null)
            return false;

        if (candidate.Length > PublicBusinessUrlPolicy.MaxUrlCharacters)
        {
            error = "Business page URL is too long.";
            return false;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var parsed) ||
            parsed.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(parsed.Host))
        {
            error = "Use a valid HTTPS business page URL.";
            return false;
        }

        if (!string.IsNullOrEmpty(parsed.UserInfo))
        {
            error = "Business page URLs cannot include credentials.";
            return false;
        }

        if (parsed.Port != 443)
        {
            error = "Use a public business page URL on the standard HTTPS port.";
            return false;
        }

        var host = parsed.IdnHost.TrimEnd('.').ToLowerInvariant();
        if (host is "localhost" or "localhost.localdomain" || BlockedHostSuffixes.Any(host.EndsWith))
        {
            error = "Use a public business page URL.";
            return false;
        }

        // VS-22 does not need IP-literal business sources. Rejecting all literals keeps
        // the user-facing URL boundary narrower than the lower-level network classifier.
        if (IPAddress.TryParse(host, out _))
        {
            error = "Use a public business hostname rather than an IP address.";
            return false;
        }

        var kind = KindFor(host);
        if (!ValidateProviderRoute(kind, host, parsed, out error))
            return false;

        var builder = new UriBuilder(parsed)
        {
            Scheme = Uri.UriSchemeHttps,
            Host = host,
            Port = -1,
            Fragment = string.Empty,
            Path = CanonicalPath(parsed.AbsolutePath),
            Query = CanonicalQuery(parsed, kind, host)
        };

        var uri = builder.Uri;
        var value = uri.AbsoluteUri;
        if (value.Length > PublicBusinessUrlPolicy.MaxUrlCharacters)
        {
            error = "Business page URL is too long.";
            return false;
        }

        canonical = new CanonicalBusinessUrl(uri, value, kind);
        return true;
    }

    public static IReadOnlyList<CanonicalBusinessUrl> CanonicalizeMany(string primary, IReadOnlyList<string>? additional)
    {
        if (additional is { Count: > MaxSources - 1 })
            throw new BusinessDiscoveryException("business_sources_too_many", $"Use at most {MaxSources} public business pages.");

        var raw = new List<string> { primary };
        if (additional is not null)
            raw.AddRange(additional.Where(value => !string.IsNullOrWhiteSpace(value)));

        if (raw.Count > MaxSources)
            throw new BusinessDiscoveryException("business_sources_too_many", $"Use at most {MaxSources} public business pages.");

        var result = new List<CanonicalBusinessUrl>(raw.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < raw.Count; index++)
        {
            if (!TryCanonicalize(raw[index], out var canonical, out var error) || canonical is null)
                throw new BusinessDiscoveryException(
                    "business_url_invalid",
                    error ?? $"Business page {index + 1} is not a supported public HTTPS business URL.");

            if (!seen.Add(canonical.Value))
                throw new BusinessDiscoveryException("business_source_duplicate", "The same public business page was added more than once.");

            result.Add(canonical);
        }

        return result;
    }

    private static bool TryExtractSingleHttpsUrl(string? rawInput, out string? candidate, out string? error)
    {
        candidate = null;
        error = null;
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            error = "Business page URL is required.";
            return false;
        }

        var sanitized = new string(rawInput
            .Where(ch => !char.IsControl(ch) || char.IsWhiteSpace(ch))
            .ToArray())
            .Trim();
        if (sanitized.Length == 0 || sanitized.Length > MaxRawInputCharacters)
        {
            error = "Business page URL is too long or empty.";
            return false;
        }

        if (Uri.TryCreate(sanitized, UriKind.Absolute, out var direct) && direct.Scheme == Uri.UriSchemeHttps)
        {
            candidate = sanitized;
            return true;
        }

        var matches = HttpsUrlRegex.Matches(sanitized);
        if (matches.Count != 1)
        {
            error = matches.Count > 1
                ? "Share one business page URL in each field."
                : "Use a valid HTTPS business page URL.";
            return false;
        }

        candidate = TrimSharePunctuation(matches[0].Value);
        return candidate.Length > 0;
    }

    private static string TrimSharePunctuation(string value) =>
        value.TrimEnd('.', ',', ';', ':', '!', ')', ']', '}');

    private static BusinessSourceKind KindFor(string host)
    {
        if (host == "food.bolt.eu") return BusinessSourceKind.BoltFood;
        if (host == "wolt.com" || host.EndsWith(".wolt.com", StringComparison.Ordinal)) return BusinessSourceKind.Wolt;
        if (host == "maps.app.goo.gl" || host == "maps.google.com" || host == "google.com" || host == "www.google.com")
            return BusinessSourceKind.GoogleMaps;
        return BusinessSourceKind.Website;
    }

    private static bool ValidateProviderRoute(BusinessSourceKind kind, string host, Uri uri, out string? error)
    {
        error = null;
        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        switch (kind)
        {
            case BusinessSourceKind.BoltFood:
            {
                var pageIndex = Array.FindIndex(segments, part => part.Equals("p", StringComparison.OrdinalIgnoreCase));
                if (pageIndex < 0 || pageIndex >= segments.Length - 1 || segments[pageIndex + 1].Length < 2)
                {
                    error = "Share the specific Bolt Food business page, not a marketplace home or browsing page.";
                    return false;
                }
                return true;
            }
            case BusinessSourceKind.Wolt:
            {
                var businessIndex = Array.FindIndex(segments, part =>
                    part.Equals("restaurant", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("venue", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("store", StringComparison.OrdinalIgnoreCase));
                if (businessIndex < 0 || businessIndex >= segments.Length - 1 || segments[businessIndex + 1].Length < 2)
                {
                    error = "Share the specific Wolt business page, not a marketplace home or browsing page.";
                    return false;
                }
                return true;
            }
            case BusinessSourceKind.GoogleMaps:
            {
                if (host == "maps.app.goo.gl")
                {
                    if (segments.Length == 1 && segments[0].Length >= 4) return true;
                    error = "Share a specific Google Maps business location link.";
                    return false;
                }

                var path = uri.AbsolutePath;
                if (path.Contains("/search", StringComparison.OrdinalIgnoreCase) && !HasGooglePlaceIdentifier(uri.Query))
                {
                    error = "Google Search links are not a specific business location. Share the Google Maps business profile/location instead.";
                    return false;
                }

                if (path.Contains("/maps/place/", StringComparison.OrdinalIgnoreCase) || HasGooglePlaceIdentifier(uri.Query))
                    return true;

                error = "Share a Google Maps link that identifies one business location.";
                return false;
            }
            default:
                return true;
        }
    }

    private static bool HasGooglePlaceIdentifier(string query)
    {
        foreach (var (key, value) in ParseQuery(query))
            if (GoogleIdentityQueryKeys.Contains(key) && !string.IsNullOrWhiteSpace(value))
                return true;
        return false;
    }

    private static string CanonicalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        return path.Length > 1 ? path.TrimEnd('/') : path;
    }

    private static string CanonicalQuery(Uri uri, BusinessSourceKind kind, string host)
    {
        if (string.IsNullOrEmpty(uri.Query)) return string.Empty;
        if (kind is BusinessSourceKind.BoltFood or BusinessSourceKind.Wolt) return string.Empty;
        if (kind == BusinessSourceKind.GoogleMaps && host == "maps.app.goo.gl") return string.Empty;

        var pairs = new List<string>();
        foreach (var (key, value) in ParseQuery(uri.Query))
        {
            if (IsTrackingKey(key)) continue;
            if (kind == BusinessSourceKind.GoogleMaps && !GoogleIdentityQueryKeys.Contains(key)) continue;
            pairs.Add(string.IsNullOrEmpty(value)
                ? Uri.EscapeDataString(key)
                : $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }
        return string.Join("&", pairs);
    }

    private static bool IsTrackingKey(string key) =>
        key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase) || TrackingKeys.Contains(key);

    private static IEnumerable<(string Key, string Value)> ParseQuery(string query)
    {
        var raw = query.TrimStart('?');
        if (raw.Length == 0) yield break;

        foreach (var part in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            var rawKey = separator < 0 ? part : part[..separator];
            var rawValue = separator < 0 ? string.Empty : part[(separator + 1)..];
            var key = DecodeQueryComponent(rawKey);
            var value = DecodeQueryComponent(rawValue);
            if (key.Length > 0) yield return (key, value);
        }
    }

    private static string DecodeQueryComponent(string value)
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
}
