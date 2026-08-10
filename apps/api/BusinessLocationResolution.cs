using System.Globalization;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed record MarketplaceBusinessName(string Value, string Confidence);

public static partial class MarketplaceBusinessIdentity
{
    public static MarketplaceBusinessName ResolveName(string provider, Uri sourceUri, string? observedName)
    {
        var cleaned = observedName?.Trim();
        if (!string.IsNullOrWhiteSpace(cleaned) && !IsProviderGeneric(provider, cleaned))
            return new MarketplaceBusinessName(cleaned, "high");

        var slug = MerchantSlug(sourceUri);
        if (!string.IsNullOrWhiteSpace(slug))
            return new MarketplaceBusinessName(HumanizeSlug(slug), "medium");

        return new MarketplaceBusinessName(cleaned ?? string.Empty, string.IsNullOrWhiteSpace(cleaned) ? "low" : "medium");
    }

    private static bool IsProviderGeneric(string provider, string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return provider switch
        {
            "bolt-food" => normalized is "bolt food" or "bolt" || normalized.StartsWith("bolt food |", StringComparison.Ordinal),
            "wolt" => normalized is "wolt" or "wolt delivery" || normalized.StartsWith("wolt |", StringComparison.Ordinal),
            _ => false
        };
    }

    private static string? MerchantSlug(Uri uri)
    {
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (!segments[i].Equals("p", StringComparison.OrdinalIgnoreCase)) continue;
            var match = MarketplaceSlugRegex().Match(Uri.UnescapeDataString(segments[i + 1]));
            if (match.Success) return match.Groups[1].Value;
        }
        return null;
    }

    private static string HumanizeSlug(string slug)
    {
        var words = slug.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant()));
        return string.Join(' ', words);
    }

    [GeneratedRegex("^\\d+-(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex MarketplaceSlugRegex();
}

public sealed record BusinessMarketMetadata(
    string CountryName,
    string CountryCode,
    string Timezone,
    string Currency,
    string CurrencyName,
    string CurrencySymbol)
{
    public static BusinessMarketMetadata Resolve(string countryCode, string timezone)
    {
        var code = countryCode.Trim().ToUpperInvariant();
        if (code.Length != 2) throw new ArgumentException("Country code must be a two-letter ISO code.", nameof(countryCode));

        RegionInfo region;
        try
        {
            region = new RegionInfo(code);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Country code must be a supported two-letter ISO code.", nameof(countryCode), ex);
        }

        var zone = timezone.Trim();
        if (string.IsNullOrWhiteSpace(zone)) throw new ArgumentException("Timezone is required.", nameof(timezone));
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(zone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ArgumentException("Timezone must be a valid IANA timezone identifier.", nameof(timezone), ex);
        }

        return new BusinessMarketMetadata(
            region.EnglishName,
            code,
            zone,
            region.ISOCurrencySymbol,
            region.CurrencyEnglishName,
            region.CurrencySymbol);
    }
}

public sealed record BusinessLocationCandidate(
    string ProviderRef,
    string Name,
    string FormattedAddress,
    double Latitude,
    double Longitude,
    string CountryCode,
    string CountryName,
    string Timezone,
    string Currency,
    string Provider);

public enum BusinessLocationResolutionState
{
    SearchRequired,
    Preselected,
    RequiresSelection
}

public sealed record BusinessLocationResolutionResult(
    BusinessLocationResolutionState State,
    IReadOnlyList<BusinessLocationCandidate> Candidates,
    BusinessLocationCandidate? Selected,
    bool CanChange);

public static class BusinessLocationResolution
{
    public static BusinessLocationResolutionResult Classify(IReadOnlyList<BusinessLocationCandidate> candidates)
    {
        if (candidates.Count == 0)
            return new BusinessLocationResolutionResult(BusinessLocationResolutionState.SearchRequired, candidates, null, true);
        if (candidates.Count == 1)
            return new BusinessLocationResolutionResult(BusinessLocationResolutionState.Preselected, candidates, candidates[0], true);
        return new BusinessLocationResolutionResult(BusinessLocationResolutionState.RequiresSelection, candidates, null, true);
    }
}

public interface IBusinessLocationProvider
{
    bool IsConfigured { get; }
    Task<IReadOnlyList<BusinessLocationCandidate>> SearchAsync(string query, CancellationToken ct);
}

public sealed class BusinessLocationProviderUnavailableException(string message) : Exception(message);

public sealed class GoogleBusinessLocationProvider(HttpClient client, IConfiguration configuration) : IBusinessLocationProvider
{
    private const int MaxResults = 5;
    private const string PlacesEndpoint = "https://places.googleapis.com/v1/places:searchText";
    private const string PlacesFieldMask = "places.id,places.displayName,places.formattedAddress,places.location,places.addressComponents";
    private const string ProviderName = "google-places";

    private string? ApiKey => configuration["GoogleMaps:ApiKey"]?.Trim();
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public async Task<IReadOnlyList<BusinessLocationCandidate>> SearchAsync(string query, CancellationToken ct)
    {
        var normalizedQuery = query.Trim();
        if (normalizedQuery.Length is < 2 or > 200)
            throw new BusinessDiscoveryException("business_location_query_invalid", "Search using a business name or address between 2 and 200 characters.");
        if (!IsConfigured)
            throw new BusinessLocationProviderUnavailableException("Business location search is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, PlacesEndpoint)
        {
            Content = JsonContent.Create(new { textQuery = normalizedQuery, pageSize = MaxResults })
        };
        request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", ApiKey);
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask", PlacesFieldMask);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Location provider returned HTTP {(int)response.StatusCode}.");

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!document.RootElement.TryGetProperty("places", out var places) || places.ValueKind != JsonValueKind.Array)
            return [];

        var candidates = new List<BusinessLocationCandidate>(MaxResults);
        foreach (var place in places.EnumerateArray())
        {
            if (candidates.Count >= MaxResults) break;
            var providerRef = String(place, "id");
            var name = NestedString(place, "displayName", "text");
            var formattedAddress = String(place, "formattedAddress");
            var latitude = NestedDouble(place, "location", "latitude");
            var longitude = NestedDouble(place, "location", "longitude");
            var countryCode = CountryCode(place);
            if (string.IsNullOrWhiteSpace(providerRef) || string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(formattedAddress) || latitude is null || longitude is null ||
                string.IsNullOrWhiteSpace(countryCode)) continue;

            var timezone = await TimezoneAsync(latitude.Value, longitude.Value, ct);
            if (string.IsNullOrWhiteSpace(timezone)) continue;
            BusinessMarketMetadata market;
            try
            {
                market = BusinessMarketMetadata.Resolve(countryCode, timezone);
            }
            catch (ArgumentException)
            {
                continue;
            }

            candidates.Add(new BusinessLocationCandidate(
                providerRef,
                name,
                formattedAddress,
                latitude.Value,
                longitude.Value,
                market.CountryCode,
                market.CountryName,
                market.Timezone,
                market.Currency,
                ProviderName));
        }
        return candidates;
    }

    private async Task<string?> TimezoneAsync(double latitude, double longitude, CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var uri = $"https://maps.googleapis.com/maps/api/timezone/json?location={latitude.ToString(CultureInfo.InvariantCulture)}%2C{longitude.ToString(CultureInfo.InvariantCulture)}&timestamp={timestamp}&key={Uri.EscapeDataString(ApiKey!)}";
        using var response = await client.GetAsync(uri, ct);
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = document.RootElement;
        if (!string.Equals(String(root, "status"), "OK", StringComparison.OrdinalIgnoreCase)) return null;
        return String(root, "timeZoneId");
    }

    private static string? CountryCode(JsonElement place)
    {
        if (!place.TryGetProperty("addressComponents", out var components) || components.ValueKind != JsonValueKind.Array) return null;
        foreach (var component in components.EnumerateArray())
        {
            if (!component.TryGetProperty("types", out var types) || types.ValueKind != JsonValueKind.Array) continue;
            if (types.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && x.GetString() == "country"))
                return String(component, "shortText")?.ToUpperInvariant();
        }
        return null;
    }

    private static string? String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : null;

    private static string? NestedString(JsonElement element, string parent, string property) =>
        element.TryGetProperty(parent, out var value) && value.ValueKind == JsonValueKind.Object ? String(value, property) : null;

    private static double? NestedDouble(JsonElement element, string parent, string property)
    {
        if (!element.TryGetProperty(parent, out var value) || value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty(property, out var number) || number.ValueKind != JsonValueKind.Number) return null;
        return number.TryGetDouble(out var result) ? result : null;
    }
}

public sealed record SearchBusinessLocationsRequest(string? Query);
public sealed record BusinessLocationResolutionResponse(
    string State,
    IReadOnlyList<BusinessLocationCandidate> Candidates,
    BusinessLocationCandidate? Selected,
    bool CanChange)
{
    public static BusinessLocationResolutionResponse From(BusinessLocationResolutionResult result) => new(
        result.State switch
        {
            BusinessLocationResolutionState.Preselected => "preselected",
            BusinessLocationResolutionState.RequiresSelection => "choose",
            _ => "search"
        },
        result.Candidates,
        result.Selected,
        result.CanChange);
}

public static class BusinessLocationEndpoints
{
    public static IEndpointRouteBuilder MapBusinessLocationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/business-locations/search", async (
            SearchBusinessLocationsRequest request,
            ClaimsPrincipal user,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(Subject(user))) return Results.Unauthorized();
            return await SearchProvider(request.Query, httpClientFactory, configuration, ct);
        }).RequireAuthorization("BusinessOwner");

        app.MapPost("/api/v1/business-discovery/{snapshotId:guid}/locations/search", async (
            Guid snapshotId,
            SearchBusinessLocationsRequest request,
            ClaimsPrincipal user,
            AtlasDbContext db,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            var subject = Subject(user);
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.ProviderSubject == subject, ct);
            if (account is null) return Results.NotFound();
            var snapshot = await db.BusinessDiscoverySnapshots.Include(x => x.Facts)
                .SingleOrDefaultAsync(x => x.Id == snapshotId && x.UserAccountId == account.Id, ct);
            if (snapshot is null) return Results.NotFound();

            var observedName = snapshot.Facts.FirstOrDefault(x => x.Key == "name")?.Value;
            var observedLocation = snapshot.Facts.FirstOrDefault(x => x.Key == "primaryLocation")?.Value;
            var query = request.Query?.Trim();
            if (string.IsNullOrWhiteSpace(query))
                query = string.Join(", ", new[] { observedName, observedLocation }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return await SearchProvider(query, httpClientFactory, configuration, ct);
        }).RequireAuthorization("BusinessOwner");

        return app;
    }

    private static async Task<IResult> SearchProvider(string? query, IHttpClientFactory httpClientFactory, IConfiguration configuration, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["query"] = ["Enter a business name or address to find its location."] }, extensions: new Dictionary<string, object?> { ["code"] = "business_location_query_required" });
        try
        {
            var provider = new GoogleBusinessLocationProvider(httpClientFactory.CreateClient(), configuration);
            var candidates = await provider.SearchAsync(query, ct);
            return Results.Ok(BusinessLocationResolutionResponse.From(BusinessLocationResolution.Classify(candidates)));
        }
        catch (BusinessLocationProviderUnavailableException)
        {
            return Results.Problem(statusCode: 503, title: "Location search unavailable", detail: "Atlas cannot search business locations right now. Try again later.", extensions: new Dictionary<string, object?> { ["code"] = "business_location_provider_unavailable" });
        }
        catch (BusinessDiscoveryException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["query"] = [ex.Message] }, extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
        }
        catch (HttpRequestException)
        {
            return Results.Problem(statusCode: 503, title: "Location search unavailable", detail: "Atlas cannot search business locations right now. Try again later.", extensions: new Dictionary<string, object?> { ["code"] = "business_location_provider_unavailable" });
        }
    }

    private static string? Subject(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
}
