using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    public static bool TryValidate(string? rawUrl, out Uri? uri, out string? error)
    {
        uri = null;
        error = null;
        if (string.IsNullOrWhiteSpace(rawUrl) ||
            !Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var parsed) ||
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

        var host = parsed.IdnHost.TrimEnd('.').ToLowerInvariant();
        if (host is "localhost" or "localhost.localdomain" || host.EndsWith(".localhost", StringComparison.Ordinal))
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
            if (b[0] == 0 || b[0] == 10 || b[0] == 127) return false;
            if (b[0] == 100 && b[1] is >= 64 and <= 127) return false;
            if (b[0] == 169 && b[1] == 254) return false;
            if (b[0] == 172 && b[1] is >= 16 and <= 31) return false;
            if (b[0] == 192 && b[1] == 0 && b[2] == 0) return false;
            if (b[0] == 192 && b[1] == 0 && b[2] == 2) return false;
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
            var b = address.GetAddressBytes();
            if ((b[0] & 0xFE) == 0xFC) return false;
            return true;
        }

        return false;
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

public static partial class PublicBusinessExtractor
{
    public static PublicBusinessSnapshot Extract(string provider, Uri sourceUri, string html, DateTimeOffset observedAt)
    {
        var sourceUrl = sourceUri.GetLeftPart(UriPartial.Path);
        var facts = new Dictionary<string, PublicBusinessFact>(StringComparer.OrdinalIgnoreCase);
        var structured = FindStructuredBusiness(html);

        string? StructuredString(string key) => structured is JsonElement value && TryGetString(value, key, out var result) ? result : null;

        var structuredName = StructuredString("name");
        var structuredDescription = StructuredString("description");
        var structuredUrl = StructuredString("url");
        var structuredPhone = StructuredString("telephone");
        var typeText = structured is JsonElement structuredValue ? ReadTypes(structuredValue) : string.Empty;
        var name = Decode(structuredName ?? FirstMatch(OgTitleRegex(), html) ?? FirstMatch(TitleRegex(), html));
        var description = Decode(structuredDescription ?? FirstMatch(OgDescriptionRegex(), html) ?? FirstMatch(MetaDescriptionRegex(), html));
        var website = Decode(structuredUrl);
        var phone = Decode(structuredPhone);

        Add("name", CleanTitle(name, provider), structuredName is null ? "medium" : "high");
        Add("description", description, structuredDescription is null ? "medium" : "high");
        Add("website", website, "high");
        Add("phone", phone, "high");

        if (structured is JsonElement business)
        {
            var address = ReadAddress(business);
            if (address.Location is not null) Add("primaryLocation", address.Location, "high");
            if (address.Country is not null) Add("country", address.Country, "high");
            var openingHours = ReadOpeningHours(business);
            if (openingHours is not null) Add("openingHours", openingHours, "high");
        }
        else
        {
            Add("primaryLocation", Decode(FirstMatch(StreetAddressRegex(), html)), "medium");
        }

        BusinessCategoryMatch category;
        if (provider is "wolt" or "bolt-food")
        {
            category = new BusinessCategoryMatch("restaurant-cafe", null, "high");
        }
        else
        {
            category = BusinessCategoryTaxonomy.Infer(string.Join(' ', new[] { typeText, name, description }.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        Add("category", category.CategoryKey, category.Confidence);
        if (category.SubcategoryKey is not null) Add("subcategory", category.SubcategoryKey, category.Confidence);

        return new PublicBusinessSnapshot(provider, sourceUrl, observedAt, facts.Values.ToList());

        void Add(string key, string? value, string confidence)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            facts[key] = new PublicBusinessFact(key, value.Trim(), provider, sourceUrl, observedAt, confidence);
        }
    }

    private static JsonElement? FindStructuredBusiness(string html)
    {
        foreach (Match match in JsonLdRegex().Matches(html))
        {
            try
            {
                using var document = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
                foreach (var candidate in EnumerateObjects(document.RootElement))
                {
                    var types = ReadTypes(candidate);
                    if (IsBusinessType(types) && TryGetString(candidate, "name", out _)) return candidate.Clone();
                }
            }
            catch (JsonException)
            {
                // Malformed public structured data is ignored; conservative fallbacks may still provide facts.
            }
        }
        return null;
    }

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            if (element.TryGetProperty("@graph", out var graph))
            {
                foreach (var item in EnumerateObjects(graph)) yield return item;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                foreach (var candidate in EnumerateObjects(item)) yield return candidate;
        }
    }

    private static bool IsBusinessType(string types)
    {
        if (types.Length == 0) return false;
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
            JsonValueKind.Array => string.Join(' ', type.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString())),
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
        if (hours.ValueKind == JsonValueKind.Array)
        {
            var values = hours.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x));
            var combined = string.Join("; ", values!);
            return combined.Length == 0 ? null : combined;
        }
        return null;
    }

    private static string? FirstMatch(Regex regex, string value)
    {
        var match = regex.Match(value);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? Decode(string? value) => string.IsNullOrWhiteSpace(value) ? null : WebUtility.HtmlDecode(value).Trim();

    private static string? CleanTitle(string? value, string provider)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var suffixes = provider switch
        {
            "wolt" => new[] { " | Wolt", " - Wolt" },
            "bolt-food" => new[] { " | Bolt Food", " - Bolt Food" },
            _ => []
        };
        foreach (var suffix in suffixes)
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) value = value[..^suffix.Length].Trim();
        return value;
    }

    [GeneratedRegex("<script[^>]+type=[\\\"']application/ld\\+json[\\\"'][^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, 500)]
    private static partial Regex JsonLdRegex();

    [GeneratedRegex("<meta[^>]+property=[\\\"']og:title[\\\"'][^>]+content=[\\\"']([^\\\"']+)[\\\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 500)]
    private static partial Regex OgTitleRegex();

    [GeneratedRegex("<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, 500)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("<meta[^>]+property=[\\\"']og:description[\\\"'][^>]+content=[\\\"']([^\\\"']+)[\\\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 500)]
    private static partial Regex OgDescriptionRegex();

    [GeneratedRegex("<meta[^>]+name=[\\\"']description[\\\"'][^>]+content=[\\\"']([^\\\"']+)[\\\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 500)]
    private static partial Regex MetaDescriptionRegex();

    [GeneratedRegex("[\\\"']streetAddress[\\\"']\\s*:\\s*[\\\"']([^\\\"']+)[\\\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 500)]
    private static partial Regex StreetAddressRegex();
}

public sealed record DiscoverBusinessRequest(string Url);
public sealed record DiscoveredBusinessField(string? Value, string Source, string Confidence, bool OwnerConfirmed = false);
public sealed record BusinessDiscoveryResponse(
    string Provider,
    string SourceUrl,
    DiscoveredBusinessField Name,
    DiscoveredBusinessField Category,
    DiscoveredBusinessField? Subcategory,
    DiscoveredBusinessField? PrimaryLocation,
    DiscoveredBusinessField? Description);

public sealed class BusinessDiscoveryService(HttpClient client)
{
    private const int MaxHtmlCharacters = 750_000;

    public async Task<BusinessDiscoveryResponse> DiscoverAsync(string rawUrl, CancellationToken ct)
    {
        if (!PublicBusinessUrlPolicy.TryValidate(rawUrl, out var uri, out var validationError) || uri is null)
            throw new BusinessDiscoveryException("business_url_invalid", validationError ?? "Use a valid HTTPS business page URL.");

        var provider = ProviderFor(uri.Host);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("AtlasBusinessDiscovery/1.0");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if ((int)response.StatusCode is >= 300 and < 400)
            throw new BusinessDiscoveryException("business_source_redirected", "The business page redirected to another location. Please use its final public URL.");
        if (!response.IsSuccessStatusCode)
            throw new BusinessDiscoveryException("business_source_unavailable", "Atlas could not read that business page right now.");

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
            throw new BusinessDiscoveryException("business_source_invalid_content", "The supplied URL is not a supported public business page.");

        var html = await response.Content.ReadAsStringAsync(ct);
        if (html.Length > MaxHtmlCharacters) html = html[..MaxHtmlCharacters];
        var snapshot = PublicBusinessExtractor.Extract(provider, uri, html, DateTimeOffset.UtcNow);

        PublicBusinessFact? Fact(string key) => snapshot.Facts.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        DiscoveredBusinessField? Field(string key)
        {
            var fact = Fact(key);
            return fact is null ? null : new DiscoveredBusinessField(fact.Value, fact.Source, fact.Confidence, fact.OwnerConfirmed);
        }

        var name = Field("name") ?? new DiscoveredBusinessField(null, provider, "low");
        var category = Field("category") ?? new DiscoveredBusinessField(BusinessCategoryTaxonomy.Generic.Key, "atlas-category-taxonomy", "low");
        return new BusinessDiscoveryResponse(provider, snapshot.SourceUrl, name, category, Field("subcategory"), Field("primaryLocation"), Field("description"));
    }

    internal static string ProviderFor(string host) => host.TrimEnd('.').ToLowerInvariant() switch
    {
        "food.bolt.eu" => "bolt-food",
        "wolt.com" or var value when value.EndsWith(".wolt.com", StringComparison.Ordinal) => "wolt",
        _ => "website"
    };
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

        app.MapPost("/api/v1/business-discovery", async (DiscoverBusinessRequest request, BusinessDiscoveryService discovery, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Url)] = ["Business page URL is required."] });
            try
            {
                return Results.Ok(await discovery.DiscoverAsync(request.Url, ct));
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
                return Results.BadRequest(new { code = "business_source_unavailable", message = "Atlas could not read that business page right now. Try again or set up manually." });
            }
        }).RequireAuthorization("BusinessOwner");

        return app;
    }
}
