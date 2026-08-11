using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed record BusinessCategoryDefinition(string Key, string Label, IReadOnlyList<string> Aliases, IReadOnlyList<BusinessCategoryDefinition> Children);
public sealed record BusinessCategoryMatch(string CategoryKey, string? SubcategoryKey, string Confidence);

public static class BusinessCategoryTaxonomy
{
    public static readonly IReadOnlyList<BusinessCategoryDefinition> Categories =
    [
        new("restaurant-cafe", "Restaurant & Café", ["restaurant", "cafe", "coffee", "bakery", "takeaway", "food"],
        [
            new("restaurant", "Restaurant", ["dining", "eatery"], []),
            new("cafe", "Café", ["coffee shop", "coffeehouse", "coffee", "cafeorcoffeeshop"], []),
            new("bakery", "Bakery", ["bakeshop", "patisserie"], []),
            new("takeaway", "Takeaway", ["fast food", "delivery kitchen"], [])
        ]),
        new("beauty-personal-care", "Beauty & Personal Care", ["salon", "barber", "spa", "nails", "beauty"], []),
        new("retail", "Retail", ["shop", "store", "grocery", "supermarket"], []),
        new("ecommerce", "Ecommerce", ["online store", "web shop", "online retail"], []),
        new("home-local-services", "Home & Local Services", ["plumber", "plumbing", "electrician", "cleaner", "repair", "landscaping"], []),
        new("professional-services", "Professional Services", ["consultant", "accountant", "agency", "lawyer", "it services"], []),
        new("fitness-wellness", "Fitness & Wellness", ["gym", "fitness", "trainer", "studio", "wellness"], []),
        new("hospitality-accommodation", "Hospitality & Accommodation", ["hotel", "guest house", "hostel", "accommodation"], [])
    ];

    public static BusinessCategoryDefinition Generic { get; } = new("generic-business", "Other business", [], []);

    public static BusinessCategoryMatch Infer(string? text)
    {
        var normalized = Normalize(text);
        if (normalized.Length == 0) return new BusinessCategoryMatch(Generic.Key, null, "low");

        foreach (var category in Categories)
        {
            foreach (var child in category.Children)
            {
                if (Signals(child).Any(signal => ContainsSignal(normalized, signal)))
                    return new BusinessCategoryMatch(category.Key, child.Key, "high");
            }
        }

        foreach (var category in Categories)
        {
            if (Signals(category).Any(signal => ContainsSignal(normalized, signal)))
                return new BusinessCategoryMatch(category.Key, null, "medium");
        }

        return new BusinessCategoryMatch(Generic.Key, null, "low");
    }

    public static bool IsKnownCategory(string key) =>
        Generic.Key.Equals(key, StringComparison.OrdinalIgnoreCase) || Categories.Any(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    public static bool IsKnownSubcategory(string categoryKey, string? subcategoryKey)
    {
        if (string.IsNullOrWhiteSpace(subcategoryKey)) return true;
        var category = Categories.FirstOrDefault(x => x.Key.Equals(categoryKey, StringComparison.OrdinalIgnoreCase));
        return category?.Children.Any(x => x.Key.Equals(subcategoryKey, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static IEnumerable<string> Signals(BusinessCategoryDefinition definition) =>
        definition.Aliases.Append(definition.Label).Append(definition.Key);

    private static bool ContainsSignal(string normalizedText, string signal)
    {
        var normalizedSignal = Normalize(signal);
        return normalizedSignal.Length >= 3 && normalizedText.Contains(normalizedSignal, StringComparison.Ordinal);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ');
        }
        return Regex.Replace(builder.ToString(), "\\s+", " ").Trim();
    }
}

public static class PublicBusinessUrlPolicy
{
    public const int MaxUrlCharacters = 2000;
    private static readonly string[] BlockedHostSuffixes = [".localhost", ".local", ".internal", ".home", ".lan", ".test"];

    public static bool TryValidate(string? rawUrl, out Uri? uri, out string? error)
    {
        uri = null;
        error = null;
        var candidate = rawUrl?.Trim();
        if (candidate?.Length > MaxUrlCharacters)
        {
            error = "Business page URL is too long.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(candidate) ||
            !Uri.TryCreate(candidate, UriKind.Absolute, out var parsed) ||
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

        if (IPAddress.TryParse(host, out var literal) && !IsPublicAddress(literal))
        {
            error = "Use a public business page URL.";
            return false;
        }

        uri = parsed;
        return true;
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) return IsPublicAddress(address.MapToIPv4());

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            if (b[0] is 0 or 10 or 127) return false;
            if (b[0] == 100 && b[1] is >= 64 and <= 127) return false;
            if (b[0] == 169 && b[1] == 254) return false;
            if (b[0] == 172 && b[1] is >= 16 and <= 31) return false;
            if (b[0] == 192 && b[1] == 0 && b[2] is 0 or 2) return false;
            if (b[0] == 192 && b[1] == 168) return false;
            if (b[0] == 198 && b[1] is 18 or 19) return false;
            if (b[0] == 198 && b[1] == 51 && b[2] == 100) return false;
            if (b[0] == 203 && b[1] == 0 && b[2] == 113) return false;
            if (b[0] >= 224) return false;
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (IPAddress.IPv6Any.Equals(address) || IPAddress.IPv6Loopback.Equals(address) || address.IsIPv6Multicast || address.IsIPv6LinkLocal) return false;
            var bytes = address.GetAddressBytes();
            if ((bytes[0] & 0xFE) == 0xFC) return false;
            return true;
        }

        return false;
    }
}

public static class PublicBusinessHttpConnector
{
    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
        if (addresses.Length == 0 || addresses.Any(address => !PublicBusinessUrlPolicy.IsPublicAddress(address)))
            throw new HttpRequestException("Public business source resolved to a blocked network address.");

        Exception? last = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                last = ex;
                socket.Dispose();
                if (ex is OperationCanceledException) throw;
            }
        }
        throw new HttpRequestException("Atlas could not connect to that public business source.", last);
    }
}

public static class PublicBusinessHtmlReader
{
    public const int MaxCharacters = 750_000;
    private const int BufferCharacters = 8192;

    public static async Task<string> ReadAsync(HttpContent content, CancellationToken ct)
    {
        await using var stream = await content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: BufferCharacters, leaveOpen: false);
        var builder = new StringBuilder(Math.Min(MaxCharacters, 64 * 1024));
        var buffer = new char[BufferCharacters];

        while (builder.Length < MaxCharacters)
        {
            var remaining = MaxCharacters - builder.Length;
            var read = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining)), ct);
            if (read == 0) break;
            builder.Append(buffer, 0, read);
        }

        return builder.ToString();
    }
}

public sealed record PublicBusinessFact(
    string Key,
    string Value,
    string Source,
    string SourceUrl,
    DateTimeOffset ObservedAt,
    string Confidence,
    string EvidenceClass = "public-observed",
    bool OwnerConfirmed = false);

public sealed record PublicBusinessSnapshot(
    string Provider,
    string SourceUrl,
    DateTimeOffset ObservedAt,
    IReadOnlyList<PublicBusinessFact> Facts);

public static class PublicBusinessExtractor
{
    public const int MaxFactValueCharacters = BusinessDiscoveryProvenance.MaxValueCharacters;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly Regex JsonLdRegex = new(@"<script[^>]+type=[""']application/ld\+json[""'][^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex OgTitleRegex = new(@"<meta[^>]+property=[""']og:title[""'][^>]+content=(?<quote>[""'])(?<value>.*?)\k<quote>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex TitleRegex = new(@"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex OgDescriptionRegex = new(@"<meta[^>]+property=[""']og:description[""'][^>]+content=(?<quote>[""'])(?<value>.*?)\k<quote>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex MetaDescriptionRegex = new(@"<meta[^>]+name=[""']description[""'][^>]+content=(?<quote>[""'])(?<value>.*?)\k<quote>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex StreetAddressRegex = new(@"[""']streetAddress[""']\s*:\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);

    public static PublicBusinessSnapshot Extract(string provider, Uri sourceUri, string html, DateTimeOffset observedAt)
    {
        var sourceUrl = sourceUri.GetLeftPart(UriPartial.Path);
        var facts = new Dictionary<string, PublicBusinessFact>(StringComparer.OrdinalIgnoreCase);
        var structured = FindStructuredBusiness(html, provider, sourceUri);

        string? StructuredString(string key) => structured is JsonElement value && TryGetString(value, key, out var result) ? result : null;

        var structuredName = StructuredString("name");
        var structuredDescription = StructuredString("description");
        var structuredUrl = StructuredString("url");
        var structuredPhone = StructuredString("telephone");
        var typeText = structured is JsonElement structuredValue ? ReadTypes(structuredValue) : string.Empty;
        var name = Decode(structuredName ?? FirstMatch(OgTitleRegex, html) ?? FirstMatch(TitleRegex, html));
        var description = Decode(structuredDescription ?? FirstMatch(OgDescriptionRegex, html) ?? FirstMatch(MetaDescriptionRegex, html));
        var cleanedName = CleanTitle(name, provider);
        var urlIdentityName = IsGenericMarketplaceTitle(cleanedName, provider) ? MarketplaceDisplayName(provider, sourceUri) : null;

        Add("name", urlIdentityName ?? cleanedName, urlIdentityName is null && structuredName is not null ? "high" : "medium");
        Add("description", description, structuredDescription is null ? "medium" : "high");
        Add("website", Decode(structuredUrl), "high");
        Add("phone", Decode(structuredPhone), "high");

        if (structured is JsonElement business)
        {
            var address = ReadAddress(business);
            Add("primaryLocation", address.Location, "high");
            Add("country", address.Country, "high");
            Add("openingHours", ReadOpeningHours(business), "high");
        }
        else
        {
            Add("primaryLocation", Decode(FirstMatch(StreetAddressRegex, html)), "medium");
        }

        var category = provider is "wolt" or "bolt-food"
            ? new BusinessCategoryMatch("restaurant-cafe", null, "high")
            : BusinessCategoryTaxonomy.Infer(string.Join(" ", new[] { typeText, name, description }.Where(x => !string.IsNullOrWhiteSpace(x))));

        Add("category", category.CategoryKey, category.Confidence);
        Add("subcategory", category.SubcategoryKey, category.Confidence);

        return new PublicBusinessSnapshot(provider, sourceUrl, observedAt, facts.Values.ToList());

        void Add(string key, string? value, string confidence)
        {
            var cleaned = value?.Trim();
            if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length > MaxFactValueCharacters) return;
            facts[key] = new PublicBusinessFact(key, cleaned, provider, sourceUrl, observedAt, confidence);
        }
    }

    private static JsonElement? FindStructuredBusiness(string html, string provider, Uri sourceUri)
    {
        var candidates = new List<JsonElement>();
        foreach (Match match in JsonLdRegex.Matches(html))
        {
            try
            {
                using var document = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
                foreach (var candidate in EnumerateObjects(document.RootElement))
                {
                    var types = ReadTypes(candidate);
                    if (IsBusinessType(types) && TryGetString(candidate, "name", out _)) candidates.Add(candidate.Clone());
                }
            }
            catch (JsonException)
            {
                // Ignore malformed public structured data and use conservative metadata fallbacks.
            }
        }

        if (candidates.Count == 0) return null;
        if (provider is not "wolt" and not "bolt-food") return candidates[0];

        foreach (var candidate in candidates)
        {
            if (!TryGetString(candidate, "url", out var candidateUrl) ||
                !Uri.TryCreate(candidateUrl, UriKind.Absolute, out var candidateUri)) continue;
            if (SameMarketplacePath(sourceUri, candidateUri)) return candidate;
        }

        var sourceIdentity = MarketplaceSourceIdentity(provider, sourceUri);
        if (sourceIdentity.Length == 0) return null;
        foreach (var candidate in candidates)
        {
            if (TryGetString(candidate, "name", out var candidateName) && IdentityMatches(sourceIdentity, candidateName))
                return candidate;
        }

        return null;
    }

    private static bool SameMarketplacePath(Uri sourceUri, Uri candidateUri)
    {
        if (!string.Equals(sourceUri.IdnHost.TrimEnd('.'), candidateUri.IdnHost.TrimEnd('.'), StringComparison.OrdinalIgnoreCase)) return false;
        return string.Equals(
            sourceUri.AbsolutePath.TrimEnd('/'),
            candidateUri.AbsolutePath.TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string MarketplaceSourceIdentity(string provider, Uri sourceUri) =>
        NormalizeIdentity(MarketplaceSourceSegment(provider, sourceUri));

    private static string MarketplaceSourceSegment(string provider, Uri sourceUri)
    {
        var segment = Uri.UnescapeDataString(sourceUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty);
        if (provider == "bolt-food") segment = Regex.Replace(segment, @"^\d+-", string.Empty, RegexOptions.CultureInvariant);
        return segment.Trim('-', '_', ' ');
    }

    private static string? MarketplaceDisplayName(string provider, Uri sourceUri)
    {
        if (provider is not "bolt-food" and not "wolt") return null;
        if (!BusinessSourceUrlPolicy.TryCanonicalize(sourceUri.AbsoluteUri, out var canonical, out _) || canonical is null) return null;
        if (provider == "bolt-food" && canonical.Kind != BusinessSourceKind.BoltFood) return null;
        if (provider == "wolt" && canonical.Kind != BusinessSourceKind.Wolt) return null;

        var canonicalUri = new Uri(canonical.Value);
        var segment = MarketplaceSourceSegment(provider, canonicalUri);
        if (string.IsNullOrWhiteSpace(segment)) return null;
        var words = Regex.Replace(segment, @"[-_]+", " ", RegexOptions.CultureInvariant);
        words = Regex.Replace(words, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
        if (words.Length == 0) return null;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(words.ToLowerInvariant());
    }

    private static bool IsGenericMarketplaceTitle(string? value, string provider)
    {
        var identity = NormalizeIdentity(value);
        return provider switch
        {
            "bolt-food" => identity is "boltfood" or "bolt",
            "wolt" => identity == "wolt",
            _ => false
        };
    }

    private static bool IdentityMatches(string sourceIdentity, string? candidateName)
    {
        var candidateIdentity = NormalizeIdentity(candidateName);
        if (sourceIdentity.Length == 0 || candidateIdentity.Length == 0) return false;
        if (string.Equals(sourceIdentity, candidateIdentity, StringComparison.Ordinal)) return true;
        var shorter = Math.Min(sourceIdentity.Length, candidateIdentity.Length);
        return shorter >= 8 &&
            (sourceIdentity.Contains(candidateIdentity, StringComparison.Ordinal) || candidateIdentity.Contains(sourceIdentity, StringComparison.Ordinal));
    }

    private static string NormalizeIdentity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
        }
        return builder.ToString();
    }

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            if (element.TryGetProperty("@graph", out var graph))
                foreach (var item in EnumerateObjects(graph)) yield return item;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                foreach (var candidate in EnumerateObjects(item)) yield return candidate;
        }
    }

    private static bool IsBusinessType(string types)
    {
        var normalized = types.ToLowerInvariant();
        return normalized.Contains("localbusiness", StringComparison.Ordinal) ||
               normalized.Contains("restaurant", StringComparison.Ordinal) ||
               normalized.Contains("cafeorcoffeeshop", StringComparison.Ordinal) ||
               normalized.Contains("bakery", StringComparison.Ordinal) ||
               normalized.Contains("store", StringComparison.Ordinal) ||
               normalized.Contains("hotel", StringComparison.Ordinal) ||
               normalized.Contains("beautysalon", StringComparison.Ordinal) ||
               normalized.Contains("healthclub", StringComparison.Ordinal) ||
               normalized.Contains("professionalservice", StringComparison.Ordinal) ||
               normalized.Contains("homeservice", StringComparison.Ordinal);
    }

    private static string ReadTypes(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var type)) return string.Empty;
        return type.ValueKind switch
        {
            JsonValueKind.String => type.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Join(" ", type.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString())),
            _ => string.Empty
        };
    }

    private static bool TryGetString(JsonElement element, string property, out string? value)
    {
        value = null;
        if (!element.TryGetProperty(property, out var found) || found.ValueKind != JsonValueKind.String) return false;
        value = found.GetString()?.Trim();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static (string? Location, string? Country) ReadAddress(JsonElement business)
    {
        if (!business.TryGetProperty("address", out var address)) return (null, null);
        if (address.ValueKind == JsonValueKind.String) return (address.GetString()?.Trim(), null);
        if (address.ValueKind != JsonValueKind.Object) return (null, null);

        var parts = new List<string>();
        foreach (var key in new[] { "streetAddress", "addressLocality", "addressRegion", "postalCode", "addressCountry" })
        {
            if (TryGetString(address, key, out var part) && part is not null) parts.Add(part);
        }
        TryGetString(address, "addressCountry", out var country);
        return (parts.Count == 0 ? null : string.Join(", ", parts), country);
    }

    private static string? ReadOpeningHours(JsonElement business)
    {
        if (!business.TryGetProperty("openingHours", out var hours)) return null;
        if (hours.ValueKind == JsonValueKind.String) return hours.GetString()?.Trim();
        if (hours.ValueKind != JsonValueKind.Array) return null;
        var values = hours.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));
        var combined = string.Join("; ", values);
        return combined.Length == 0 ? null : combined;
    }

    private static string? FirstMatch(Regex regex, string value)
    {
        var match = regex.Match(value);
        if (!match.Success) return null;
        var namedValue = match.Groups["value"];
        return (namedValue.Success ? namedValue.Value : match.Groups[1].Value).Trim();
    }

    private static string? Decode(string? value) => string.IsNullOrWhiteSpace(value) ? null : WebUtility.HtmlDecode(value).Trim();

    private static string? CleanTitle(string? value, string provider)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var suffixes = provider switch
        {
            "wolt" => new[] { " | Wolt", " - Wolt" },
            "bolt-food" => new[] { " | Bolt Food", " - Bolt Food" },
            _ => Array.Empty<string>()
        };
        foreach (var suffix in suffixes)
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) value = value[..^suffix.Length].Trim();
        return value;
    }
}

public sealed record DiscoverBusinessRequest(string Url, IReadOnlyList<string>? AdditionalUrls = null);
public sealed record BusinessDiscoveryResponse(Guid SnapshotId, string Provider, string SourceUrl, DateTimeOffset ObservedAt, IReadOnlyList<PublicBusinessFact> Facts)
{
    public static BusinessDiscoveryResponse From(BusinessDiscoverySnapshot snapshot) => new(
        snapshot.Id,
        snapshot.Provider,
        snapshot.SourceUrl,
        snapshot.ObservedAt,
        snapshot.Facts.Select(x => new PublicBusinessFact(x.Key, x.Value, x.Source, x.SourceUrl, x.ObservedAt, x.Confidence, x.EvidenceClass, x.OwnerConfirmed)).ToList());
}

public sealed class BusinessDiscoveryService(HttpClient client)
{
    public async Task<PublicBusinessSnapshot> DiscoverAsync(string rawUrl, CancellationToken ct)
    {
        if (!PublicBusinessUrlPolicy.TryValidate(rawUrl, out var uri, out var validationError) || uri is null)
            throw new BusinessDiscoveryException("business_url_invalid", validationError ?? "Use a valid HTTPS business page URL.");

        var provider = ProviderFor(uri.Host);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("AtlasBusinessDiscovery/1.0");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if ((int)response.StatusCode is >= 300 and < 400)
            throw new BusinessDiscoveryException("business_source_redirected", "The business page redirected. Use its final public URL or set up manually.");
        if (!response.IsSuccessStatusCode)
            throw new BusinessDiscoveryException("business_source_unavailable", "Atlas could not read that business page right now.");

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
            throw new BusinessDiscoveryException("business_source_invalid_content", "The supplied URL is not a supported public business page.");

        var html = await PublicBusinessHtmlReader.ReadAsync(response.Content, ct);
        var snapshot = PublicBusinessExtractor.Extract(provider, uri, html, DateTimeOffset.UtcNow);
        var usefulFacts = snapshot.Facts.Where(x => x.Key is not "category" || x.Value != BusinessCategoryTaxonomy.Generic.Key).ToList();
        if (usefulFacts.Count == 0)
            throw new BusinessDiscoveryException("business_source_no_facts", "Atlas could not find useful business details on that page. You can set up manually instead.");
        return snapshot;
    }

    internal static string ProviderFor(string host)
    {
        var value = host.TrimEnd('.').ToLowerInvariant();
        if (value == "food.bolt.eu") return "bolt-food";
        if (value == "wolt.com" || value.EndsWith(".wolt.com", StringComparison.Ordinal)) return "wolt";
        return "website";
    }
}

public sealed class BusinessDiscoveryException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public static class BusinessDiscoveryEndpoints
{
    public static IEndpointRouteBuilder MapBusinessDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/business-categories", () => Results.Ok(new
        {
            categories = BusinessCategoryTaxonomy.Categories,
            fallback = BusinessCategoryTaxonomy.Generic
        })).RequireAuthorization("BusinessOwner");

        app.MapPost("/api/v1/business-discovery", async (
            DiscoverBusinessRequest request,
            ClaimsPrincipal user,
            BusinessDiscoveryService pageDiscovery,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Url)] = ["Business page URL is required."] });
            var subject = Subject(user);
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            try
            {
                var discovery = new MultiSourceBusinessDiscoveryService(pageDiscovery, httpClientFactory, configuration);
                var reconciliation = await discovery.DiscoverAsync(request.Url, request.AdditionalUrls, ct);
                var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.ProviderSubject == subject, ct);
                if (account is null)
                {
                    account = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = subject, CreatedAt = DateTimeOffset.UtcNow };
                    db.UserAccounts.Add(account);
                }
                var snapshot = BusinessDiscoverySnapshot.Create(account.Id, reconciliation);
                foreach (var fact in snapshot.Facts)
                {
                    fact.SnapshotId = snapshot.Id;
                    fact.Snapshot = snapshot;
                }
                db.BusinessDiscoverySnapshots.Add(snapshot);
                db.AuditRecords.Add(AuditRecord.Create(account.Id, null, "business.discovery.created"));
                await db.SaveChangesAsync(ct);
                return Results.Ok(BusinessDiscoveryResponse.From(snapshot));
            }
            catch (BusinessDiscoveryException ex)
            {
                return Results.BadRequest(new { code = ex.Code, message = ex.Message });
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                return Results.BadRequest(new { code = "business_source_timeout", message = "Atlas could not read that business page in time. Try again or set up manually." });
            }
            catch (HttpRequestException)
            {
                return Results.BadRequest(new { code = "business_source_unavailable", message = "Atlas could not read that business page safely right now. Try again or set up manually." });
            }
        }).RequireAuthorization("BusinessOwner");

        app.MapPost("/api/v1/businesses/from-discovery", async (CreateBusinessFromDiscoveryRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var subject = Subject(user);
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            try
            {
                var business = await BusinessDiscoveryBusinessCreator.CreateAsync(db, subject, request, ct);
                return Results.Created($"/api/v1/businesses/{business.Id}", business);
            }
            catch (BusinessDiscoveryValidationException ex)
            {
                return Results.ValidationProblem(ex.Errors, extensions: new Dictionary<string, object?> { ["code"] = "business_discovery_invalid" });
            }
            catch (BusinessDiscoveryException ex) when (ex.Code == "initial_business_exists")
            {
                return Results.Conflict(new { code = ex.Code, message = ex.Message });
            }
            catch (BusinessDiscoveryException ex)
            {
                return Results.BadRequest(new { code = ex.Code, message = ex.Message });
            }
        }).RequireAuthorization("BusinessOwner");

        return app;
    }

    private static string? Subject(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
}
