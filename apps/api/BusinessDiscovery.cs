using System.Net;
using System.Text.RegularExpressions;

namespace Atlas.Api;

public sealed record BusinessCategoryDefinition(string Key, string Label, IReadOnlyList<string> Aliases, IReadOnlyList<BusinessCategoryDefinition> Children);

public static class BusinessCategoryTaxonomy
{
    public static readonly IReadOnlyList<BusinessCategoryDefinition> Categories =
    [
        new("restaurant-cafe", "Restaurant & Café", ["restaurant", "cafe", "coffee", "bakery", "takeaway", "food"],
        [
            new("restaurant", "Restaurant", ["dining", "eatery"], []),
            new("cafe", "Café", ["coffee shop", "coffeehouse"], []),
            new("bakery", "Bakery", ["bakeshop", "patisserie"], []),
            new("takeaway", "Takeaway", ["fast food", "delivery kitchen"], [])
        ]),
        new("beauty-personal-care", "Beauty & Personal Care", ["salon", "barber", "spa", "nails", "beauty"], []),
        new("retail", "Retail", ["shop", "store", "grocery", "supermarket"], []),
        new("ecommerce", "Ecommerce", ["online store", "web shop", "online retail"], []),
        new("home-local-services", "Home & Local Services", ["plumber", "electrician", "cleaner", "repair", "landscaping"], []),
        new("professional-services", "Professional Services", ["consultant", "accountant", "agency", "lawyer", "it services"], []),
        new("fitness-wellness", "Fitness & Wellness", ["gym", "fitness", "trainer", "studio", "wellness"], []),
        new("hospitality-accommodation", "Hospitality & Accommodation", ["hotel", "guest house", "hostel", "accommodation"], [])
    ];

    public static BusinessCategoryDefinition Generic { get; } = new("generic-business", "Other business", [], []);
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

public sealed partial class BusinessDiscoveryService(HttpClient client)
{
    private const int MaxHtmlCharacters = 750_000;

    public async Task<BusinessDiscoveryResponse> DiscoverAsync(string rawUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new BusinessDiscoveryException("business_url_invalid", "Use a valid HTTPS business page URL.");

        var provider = ProviderFor(uri.Host);
        if (provider is null)
            throw new BusinessDiscoveryException("business_source_unsupported", "This source is not supported yet. Use a Bolt Food or Wolt business page, or continue manually.");

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

        var name = Decode(FirstMatch(OgTitleRegex(), html) ?? FirstMatch(TitleRegex(), html));
        var description = Decode(FirstMatch(OgDescriptionRegex(), html) ?? FirstMatch(MetaDescriptionRegex(), html));
        var location = Decode(FirstMatch(StreetAddressRegex(), html));

        return new BusinessDiscoveryResponse(
            provider,
            uri.GetLeftPart(UriPartial.Path),
            new DiscoveredBusinessField(CleanTitle(name, provider), provider, string.IsNullOrWhiteSpace(name) ? "low" : "high"),
            new DiscoveredBusinessField("restaurant-cafe", "atlas-category-taxonomy", "high"),
            new DiscoveredBusinessField("restaurant", "atlas-category-taxonomy", "medium"),
            string.IsNullOrWhiteSpace(location) ? null : new DiscoveredBusinessField(location, provider, "medium"),
            string.IsNullOrWhiteSpace(description) ? null : new DiscoveredBusinessField(description, provider, "medium"));
    }

    private static string? ProviderFor(string host) => host.ToLowerInvariant() switch
    {
        "food.bolt.eu" => "bolt-food",
        "wolt.com" => "wolt",
        _ => null
    };

    private static string? FirstMatch(Regex regex, string value)
    {
        var match = regex.Match(value);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? Decode(string? value) => string.IsNullOrWhiteSpace(value) ? null : WebUtility.HtmlDecode(value).Trim();

    private static string? CleanTitle(string? value, string provider)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var suffixes = provider == "wolt" ? new[] { " | Wolt", " - Wolt" } : new[] { " | Bolt Food", " - Bolt Food" };
        foreach (var suffix in suffixes)
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) value = value[..^suffix.Length].Trim();
        return value;
    }

    [GeneratedRegex("<meta[^>]+property=[\"']og:title[\"'][^>]+content=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 500)]
    private static partial Regex OgTitleRegex();

    [GeneratedRegex("<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, 500)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("<meta[^>]+property=[\"']og:description[\"'][^>]+content=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 500)]
    private static partial Regex OgDescriptionRegex();

    [GeneratedRegex("<meta[^>]+name=[\"']description[\"'][^>]+content=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 500)]
    private static partial Regex MetaDescriptionRegex();

    [GeneratedRegex("[\"']streetAddress[\"']\\s*:\\s*[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 500)]
    private static partial Regex StreetAddressRegex();
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
        }).RequireAuthorization("BusinessOwner");

        return app;
    }
}
