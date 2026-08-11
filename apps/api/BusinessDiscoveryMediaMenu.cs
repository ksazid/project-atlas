using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Atlas.Api;

public sealed record PublicBusinessMedia(
    string Kind,
    string RemoteUrl,
    string Source,
    string SourceUrl,
    DateTimeOffset ObservedAt,
    string Confidence,
    string EvidenceClass = "public-observed",
    bool OwnerConfirmed = false,
    string? AltText = null,
    int SourceOrder = 0);

public sealed record PublicBusinessOffering(
    string Kind,
    string? Section,
    string Name,
    string? Description,
    decimal? Price,
    string? Currency,
    string Source,
    string SourceUrl,
    DateTimeOffset ObservedAt,
    string Confidence,
    string EvidenceClass = "public-observed",
    bool OwnerConfirmed = false,
    int SourceOrder = 0);

public sealed record PublicBusinessMediaMenuExtraction(
    IReadOnlyList<PublicBusinessMedia> Media,
    IReadOnlyList<PublicBusinessOffering> Offerings,
    string? MenuUrl);

public static class PublicBusinessMediaMenuExtractor
{
    public const int MaxMediaPerSource = 24;
    public const int MaxOfferingsPerSource = 250;
    private const int MaxNameCharacters = 240;
    private const int MaxSectionCharacters = 240;
    private const int MaxDescriptionCharacters = 2000;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly Regex JsonLdRegex = new(@"<script[^>]+type=[""']application/ld\+json[""'][^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex OgImageRegex = new(@"<meta[^>]+property=[""']og:image[""'][^>]+content=(?<quote>[""'])(?<value>.*?)\k<quote>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);

    public static PublicBusinessMediaMenuExtraction Extract(string provider, Uri sourceUri, string html, DateTimeOffset observedAt)
    {
        var sourceUrl = sourceUri.GetLeftPart(UriPartial.Path);
        var media = new Dictionary<string, PublicBusinessMedia>(StringComparer.OrdinalIgnoreCase);
        var offerings = new Dictionary<string, PublicBusinessOffering>(StringComparer.OrdinalIgnoreCase);
        string? menuUrl = null;
        var foundStructuredImage = false;

        foreach (var root in StructuredRoots(html))
        {
            foreach (var business in EnumerateObjects(root).Where(IsBusinessObject))
            {
                if (business.TryGetProperty("image", out var images))
                {
                    foreach (var candidate in ImageUrls(images))
                    {
                        if (media.Count >= MaxMediaPerSource) break;
                        if (!TryCanonicalPublicUrl(sourceUri, candidate.Url, out var canonical)) continue;
                        foundStructuredImage = true;
                        media.TryAdd(canonical, new PublicBusinessMedia(
                            "business-image", canonical, provider, sourceUrl, observedAt, "high",
                            AltText: candidate.AltText));
                    }
                }

                foreach (var menu in MenuValues(business))
                {
                    ReadMenu(menu, null);
                    if (offerings.Count >= MaxOfferingsPerSource) break;
                }
            }
        }

        if (!foundStructuredImage)
        {
            var fallback = FirstMatch(OgImageRegex, html);
            if (TryCanonicalPublicUrl(sourceUri, fallback, out var canonical))
            {
                media.TryAdd(canonical, new PublicBusinessMedia(
                    "business-image", canonical, provider, sourceUrl, observedAt, "medium"));
            }
        }

        return new PublicBusinessMediaMenuExtraction(media.Values.ToList(), offerings.Values.ToList(), menuUrl);

        void ReadMenu(JsonElement element, string? inheritedSection)
        {
            if (offerings.Count >= MaxOfferingsPerSource) return;

            if (element.ValueKind == JsonValueKind.String)
            {
                CaptureMenuUrl(element.GetString());
                return;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    ReadMenu(item, inheritedSection);
                    if (offerings.Count >= MaxOfferingsPerSource) break;
                }
                return;
            }

            if (element.ValueKind != JsonValueKind.Object) return;

            if (TryGetString(element, "url", out var url)) CaptureMenuUrl(url);

            var types = ReadTypes(element);
            var section = inheritedSection;
            if (types.Contains("MenuSection", StringComparison.OrdinalIgnoreCase) &&
                TryGetString(element, "name", out var sectionName) &&
                IsBounded(sectionName, MaxSectionCharacters))
                section = sectionName;

            if (types.Contains("MenuItem", StringComparison.OrdinalIgnoreCase))
                CaptureOffering(element, section);

            foreach (var property in new[] { "hasMenuSection", "hasMenuItem", "itemListElement" })
            {
                if (element.TryGetProperty(property, out var child)) ReadMenu(child, section);
                if (offerings.Count >= MaxOfferingsPerSource) break;
            }
        }

        void CaptureOffering(JsonElement item, string? section)
        {
            if (offerings.Count >= MaxOfferingsPerSource || !TryGetString(item, "name", out var name) || !IsBounded(name, MaxNameCharacters)) return;
            TryGetString(item, "description", out var description);
            if (!IsBounded(description, MaxDescriptionCharacters)) description = null;
            var (price, currency) = ReadOffer(item);
            var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToUpperInvariant();
            if (normalizedCurrency is { Length: > 3 }) normalizedCurrency = null;

            var offering = new PublicBusinessOffering(
                "menu-item",
                IsBounded(section, MaxSectionCharacters) ? section?.Trim() : null,
                name.Trim(),
                description?.Trim(),
                price,
                normalizedCurrency,
                provider,
                sourceUrl,
                observedAt,
                "high");

            offerings.TryAdd(OfferingKey(offering), offering);
        }

        void CaptureMenuUrl(string? value)
        {
            if (menuUrl is not null) return;
            if (TryCanonicalPublicUrl(sourceUri, value, out var canonical)) menuUrl = canonical;
        }
    }

    private static IEnumerable<JsonElement> StructuredRoots(string html)
    {
        foreach (Match match in JsonLdRegex.Matches(html))
        {
            JsonElement? root = null;
            try
            {
                using var document = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
                root = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                // Ignore malformed structured data and keep the existing conservative discovery fallback.
            }

            if (root is JsonElement value) yield return value;
        }
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

    private static bool IsBusinessObject(JsonElement element)
    {
        var types = ReadTypes(element);
        return types.Contains("LocalBusiness", StringComparison.OrdinalIgnoreCase) ||
               types.Contains("Restaurant", StringComparison.OrdinalIgnoreCase) ||
               types.Contains("CafeOrCoffeeShop", StringComparison.OrdinalIgnoreCase) ||
               types.Contains("Bakery", StringComparison.OrdinalIgnoreCase) ||
               types.Contains("Store", StringComparison.OrdinalIgnoreCase) ||
               types.Contains("Hotel", StringComparison.OrdinalIgnoreCase) ||
               types.Contains("BeautySalon", StringComparison.OrdinalIgnoreCase) ||
               types.Contains("HealthClub", StringComparison.OrdinalIgnoreCase) ||
               types.Contains("ProfessionalService", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<JsonElement> MenuValues(JsonElement business)
    {
        if (business.TryGetProperty("hasMenu", out var hasMenu)) yield return hasMenu;
        if (business.TryGetProperty("menu", out var menu)) yield return menu;
    }

    private static IEnumerable<(string Url, string? AltText)> ImageUrls(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            if (!string.IsNullOrWhiteSpace(value)) yield return (value, null);
            yield break;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                foreach (var candidate in ImageUrls(item)) yield return candidate;
            yield break;
        }

        if (element.ValueKind != JsonValueKind.Object) yield break;
        string? url = null;
        if (TryGetString(element, "contentUrl", out var contentUrl)) url = contentUrl;
        else if (TryGetString(element, "url", out var objectUrl)) url = objectUrl;
        TryGetString(element, "caption", out var caption);
        if (!string.IsNullOrWhiteSpace(url)) yield return (url, caption);
    }

    private static (decimal? Price, string? Currency) ReadOffer(JsonElement item)
    {
        if (!item.TryGetProperty("offers", out var offers)) return (null, null);
        foreach (var offer in Objects(offers))
        {
            decimal? price = null;
            if (offer.TryGetProperty("price", out var priceElement))
            {
                if (priceElement.ValueKind == JsonValueKind.Number && priceElement.TryGetDecimal(out var numeric) && numeric >= 0) price = numeric;
                else if (priceElement.ValueKind == JsonValueKind.String &&
                         decimal.TryParse(priceElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0) price = parsed;
            }
            TryGetString(offer, "priceCurrency", out var currency);
            if (price is not null || !string.IsNullOrWhiteSpace(currency)) return (price, currency);
        }
        return (null, null);
    }

    private static IEnumerable<JsonElement> Objects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object) yield return element;
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray())
                if (item.ValueKind == JsonValueKind.Object) yield return item;
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

    private static bool TryCanonicalPublicUrl(Uri sourceUri, string? candidate, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate)) return false;
        var decoded = WebUtility.HtmlDecode(candidate).Trim();
        if (!Uri.TryCreate(decoded, UriKind.Absolute, out var absolute) && !Uri.TryCreate(sourceUri, decoded, out absolute)) return false;
        if (!PublicBusinessUrlPolicy.TryValidate(absolute.ToString(), out var validated, out _) || validated is null) return false;
        var builder = new UriBuilder(validated) { Fragment = string.Empty };
        canonical = builder.Uri.AbsoluteUri;
        return canonical.Length <= PublicBusinessUrlPolicy.MaxUrlCharacters;
    }

    private static string? FirstMatch(Regex regex, string value)
    {
        var match = regex.Match(value);
        if (!match.Success) return null;
        var namedValue = match.Groups["value"];
        return (namedValue.Success ? namedValue.Value : match.Groups[1].Value).Trim();
    }

    private static bool IsBounded(string? value, int max) => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= max;

    private static string OfferingKey(PublicBusinessOffering offering) => string.Join("|",
        offering.Kind.Trim().ToLowerInvariant(),
        offering.Section?.Trim().ToLowerInvariant() ?? string.Empty,
        offering.Name.Trim().ToLowerInvariant(),
        offering.Price?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        offering.Currency?.Trim().ToUpperInvariant() ?? string.Empty);
}
