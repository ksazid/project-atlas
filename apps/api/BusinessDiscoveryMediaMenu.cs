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

public static class PublicBusinessMediaMenuCoverage
{
    public const string Structured = "structured";
    public const string SemanticHtml = "semantic-html";
    public const string EmbeddedPublicState = "embedded-public-state";
    public const string MediaOnly = "media-only";
    public const string RendererRequired = "renderer-required";
    public const string None = "none";
}

public sealed record PublicBusinessMediaMenuExtraction(
    IReadOnlyList<PublicBusinessMedia> Media,
    IReadOnlyList<PublicBusinessOffering> Offerings,
    string? MenuUrl,
    string Coverage);

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
    private static readonly Regex BoltCategoryRegex = new(@"<h2\b[^>]*class=[""'][^""']*\bprovider-menu-category-title\b[^""']*[""'][^>]*>(?<section>.*?)</h2>(?<body>.*?)(?=<h2\b[^>]*class=[""'][^""']*\bprovider-menu-category-title\b[^""']*[""']|</body>|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex BoltDishRegex = new(@"<li\b[^>]*class=[""'][^""']*\bprovider-menu-dish\b[^""']*[""'][^>]*>(?<dish>.*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex BoltDescriptionRegex = new(@"<p\b[^>]*class=[""'][^""']*\bprovider-menu-dish-description\b[^""']*[""'][^>]*>(?<value>.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex BoltPriceRegex = new(@"<span\b[^>]*class=[""'][^""']*\bprovider-menu-dish-price\b[^""']*[""'][^>]*>(?<value>.*?)</span>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex BoltImageRegex = new(@"<img\b(?<attrs>[^>]*)>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex PriceNumberRegex = new(@"(?<amount>\d+(?:[.,]\d{1,2})?)", RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly string[] RendererMarkers =
    [
        "javascript is not enabled",
        "please enable javascript",
        "requires javascript"
    ];

    public static PublicBusinessMediaMenuExtraction Extract(string provider, Uri sourceUri, string html, DateTimeOffset observedAt)
    {
        var sourceUrl = sourceUri.GetLeftPart(UriPartial.Path);
        var media = new Dictionary<string, PublicBusinessMedia>(StringComparer.OrdinalIgnoreCase);
        var offerings = new Dictionary<string, PublicBusinessOffering>(StringComparer.OrdinalIgnoreCase);
        string? menuUrl = null;
        var foundStructuredImage = false;
        var foundStructuredContribution = false;
        var foundSemanticContribution = false;

        foreach (var root in StructuredRoots(html))
        {
            var graphById = BuildGraphIndex(root);
            foreach (var business in EnumerateObjects(root).Where(IsBusinessObject))
            {
                if (business.TryGetProperty("image", out var images))
                {
                    foreach (var candidate in ImageUrls(images))
                    {
                        if (media.Count >= MaxMediaPerSource) break;
                        if (!TryCanonicalPublicUrl(sourceUri, candidate.Url, out var canonical)) continue;
                        foundStructuredImage = true;
                        foundStructuredContribution = true;
                        media.TryAdd(canonical, new PublicBusinessMedia(
                            "business-image", canonical, provider, sourceUrl, observedAt, "high",
                            AltText: candidate.AltText));
                    }
                }

                var visited = new HashSet<string>(StringComparer.Ordinal);
                foreach (var menu in MenuValues(business))
                {
                    ReadMenu(menu, null, graphById, visited);
                    if (offerings.Count >= MaxOfferingsPerSource) break;
                }
            }
        }

        if (provider.Equals("bolt-food", StringComparison.OrdinalIgnoreCase))
            CaptureBoltSemanticMenu();

        if (!foundStructuredImage)
        {
            var fallback = FirstMatch(OgImageRegex, html);
            if (TryCanonicalPublicUrl(sourceUri, fallback, out var canonical))
            {
                media.TryAdd(canonical, new PublicBusinessMedia(
                    "business-image", canonical, provider, sourceUrl, observedAt, "medium"));
            }
        }

        var coverage = foundStructuredContribution
            ? PublicBusinessMediaMenuCoverage.Structured
            : foundSemanticContribution
                ? PublicBusinessMediaMenuCoverage.SemanticHtml
                : media.Count > 0
                    ? PublicBusinessMediaMenuCoverage.MediaOnly
                    : IsSupportedRendererProvider(provider) && HasRendererMarker(html)
                        ? PublicBusinessMediaMenuCoverage.RendererRequired
                        : PublicBusinessMediaMenuCoverage.None;

        return new PublicBusinessMediaMenuExtraction(
            media.Values.Take(MaxMediaPerSource).ToList(),
            offerings.Values.ToList(),
            menuUrl,
            coverage);

        void ReadMenu(
            JsonElement input,
            string? inheritedSection,
            IReadOnlyDictionary<string, JsonElement> graphById,
            HashSet<string> visited)
        {
            if (offerings.Count >= MaxOfferingsPerSource) return;

            if (input.ValueKind == JsonValueKind.String)
            {
                CaptureMenuUrl(input.GetString());
                return;
            }

            if (input.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in input.EnumerateArray())
                {
                    ReadMenu(item, inheritedSection, graphById, visited);
                    if (offerings.Count >= MaxOfferingsPerSource) break;
                }
                return;
            }

            if (input.ValueKind != JsonValueKind.Object) return;

            var element = ResolveReference(input, graphById);
            if (TryGetString(element, "@id", out var id) && id is not null && !visited.Add(id)) return;

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
                if (element.TryGetProperty(property, out var child)) ReadMenu(child, section, graphById, visited);
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

            AddOffering(new PublicBusinessOffering(
                "menu-item",
                IsBounded(section, MaxSectionCharacters) ? section?.Trim() : null,
                name.Trim(),
                description?.Trim(),
                price,
                normalizedCurrency,
                provider,
                sourceUrl,
                observedAt,
                "high"));
            foundStructuredContribution = true;

            if (item.TryGetProperty("image", out var itemImages))
            {
                foreach (var candidate in ImageUrls(itemImages))
                {
                    if (media.Count >= MaxMediaPerSource) break;
                    if (!TryCanonicalPublicUrl(sourceUri, candidate.Url, out var canonical)) continue;
                    media.TryAdd(canonical, new PublicBusinessMedia(
                        "menu-item-image",
                        canonical,
                        provider,
                        sourceUrl,
                        observedAt,
                        "high",
                        AltText: name.Trim()));
                }
            }
        }

        void CaptureBoltSemanticMenu()
        {
            foreach (Match categoryMatch in BoltCategoryRegex.Matches(html))
            {
                if (offerings.Count >= MaxOfferingsPerSource) break;
                var section = HtmlText(categoryMatch.Groups["section"].Value);
                if (!IsBounded(section, MaxSectionCharacters)) section = null;

                foreach (Match dishMatch in BoltDishRegex.Matches(categoryMatch.Groups["body"].Value))
                {
                    if (offerings.Count >= MaxOfferingsPerSource) break;
                    var dish = dishMatch.Groups["dish"].Value;
                    var imageMatch = BoltImageRegex.Match(dish);
                    var imageAttributes = imageMatch.Success ? imageMatch.Groups["attrs"].Value : string.Empty;
                    var name = HtmlText(AttributeValue(imageAttributes, "alt"));
                    if (string.IsNullOrWhiteSpace(name) || !IsBounded(name, MaxNameCharacters)) continue;

                    var descriptionMatch = BoltDescriptionRegex.Match(dish);
                    var description = descriptionMatch.Success ? HtmlText(descriptionMatch.Groups["value"].Value) : null;
                    if (!IsBounded(description, MaxDescriptionCharacters)) description = null;

                    var priceMatch = BoltPriceRegex.Match(dish);
                    var priceText = priceMatch.Success ? HtmlText(priceMatch.Groups["value"].Value) : null;
                    var (price, currency) = ReadDisplayPrice(priceText);

                    AddOffering(new PublicBusinessOffering(
                        "menu-item",
                        section,
                        name,
                        description,
                        price,
                        currency,
                        provider,
                        sourceUrl,
                        observedAt,
                        "high"));
                    foundSemanticContribution = true;

                    if (media.Count < MaxMediaPerSource)
                    {
                        var imageUrl = AttributeValue(imageAttributes, "src");
                        if (TryCanonicalPublicUrl(sourceUri, imageUrl, out var canonicalImage))
                        {
                            media.TryAdd(canonicalImage, new PublicBusinessMedia(
                                "menu-item-image",
                                canonicalImage,
                                provider,
                                sourceUrl,
                                observedAt,
                                "high",
                                AltText: name));
                            foundSemanticContribution = true;
                        }
                    }
                }
            }
        }

        void AddOffering(PublicBusinessOffering offering)
        {
            if (offerings.Count >= MaxOfferingsPerSource) return;
            offerings.TryAdd(OfferingKey(offering), offering);
        }

        void CaptureMenuUrl(string? value)
        {
            if (menuUrl is not null) return;
            if (TryCanonicalPublicUrl(sourceUri, value, out var canonical))
            {
                menuUrl = canonical;
                foundStructuredContribution = true;
            }
        }
    }

    private static Dictionary<string, JsonElement> BuildGraphIndex(JsonElement root)
    {
        var graph = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var candidate in EnumerateObjects(root))
        {
            if (TryGetString(candidate, "@id", out var id) && id is not null)
                graph.TryAdd(id, candidate);
        }
        return graph;
    }

    private static JsonElement ResolveReference(JsonElement element, IReadOnlyDictionary<string, JsonElement> graphById)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            TryGetString(element, "@id", out var id) &&
            id is not null &&
            graphById.TryGetValue(id, out var resolved))
            return resolved;
        return element;
    }

    private static bool IsSupportedRendererProvider(string provider) =>
        provider.Equals("bolt-food", StringComparison.OrdinalIgnoreCase) ||
        provider.Equals("wolt", StringComparison.OrdinalIgnoreCase);

    private static bool HasRendererMarker(string html) =>
        RendererMarkers.Any(marker => html.Contains(marker, StringComparison.OrdinalIgnoreCase));

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

    private static (decimal? Price, string? Currency) ReadDisplayPrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (null, null);
        var match = PriceNumberRegex.Match(value);
        decimal? price = null;
        if (match.Success && decimal.TryParse(match.Groups["amount"].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
            price = parsed;
        var currency = value.Contains('€') ? "EUR" : value.Contains('£') ? "GBP" : value.Contains('$') ? "USD" : null;
        return (price, currency);
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

    private static string? AttributeValue(string attributes, string name)
    {
        if (string.IsNullOrWhiteSpace(attributes)) return null;
        var regex = new Regex($@"\b{Regex.Escape(name)}\s*=\s*(?<quote>[""'])(?<value>.*?)\k<quote>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexTimeout);
        var match = regex.Match(attributes);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["value"].Value).Trim() : null;
    }

    private static string? HtmlText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var decoded = WebUtility.HtmlDecode(value);
        var withoutTags = HtmlTagRegex.Replace(decoded, " ");
        var normalized = WhitespaceRegex.Replace(withoutTags, " ").Trim();
        return normalized.Length == 0 ? null : normalized;
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