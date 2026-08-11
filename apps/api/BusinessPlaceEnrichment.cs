using System.Text.Json;

namespace Atlas.Api;

public sealed record BusinessPlaceAttribution(string Provider, string? ProviderUri);

public sealed record BusinessPlaceEnrichment(
    string ProviderRef,
    DateTimeOffset ObservedAt,
    IReadOnlyList<string> OperatingChannels,
    bool? Reservable,
    IReadOnlyList<string> ServicePeriods,
    string? PricePosition,
    IReadOnlyList<string> OpeningHours,
    IReadOnlyList<BusinessPlaceAttribution> Attributions);

public interface IBusinessPlaceEnrichmentProvider
{
    bool IsConfigured { get; }
    Task<BusinessPlaceEnrichment?> GetAsync(string providerRef, CancellationToken ct);
}

public sealed class GoogleBusinessPlaceEnrichmentProvider(HttpClient client, IConfiguration configuration)
    : IBusinessPlaceEnrichmentProvider
{
    internal const string PlaceDetailsFieldMask =
        "id,dineIn,takeout,delivery,reservable,servesBreakfast,servesBrunch,servesLunch,servesDinner,priceLevel,regularOpeningHours,attributions";

    private string? ApiKey => configuration["GoogleMaps:ApiKey"]?.Trim();
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public async Task<BusinessPlaceEnrichment?> GetAsync(string providerRef, CancellationToken ct)
    {
        var placeId = providerRef.Trim();
        if (placeId.Length is < 1 or > 2048)
            throw new BusinessDiscoveryException("business_place_ref_invalid", "That business location reference is invalid.");
        if (!IsConfigured) return null;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(placeId)}");
        request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", ApiKey);
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask", PlaceDetailsFieldMask);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(
            stream,
            new JsonDocumentOptions { MaxDepth = 24 },
            ct);

        return GoogleBusinessPlaceEnrichmentMapper.Map(placeId, document.RootElement, DateTimeOffset.UtcNow);
    }
}

internal static class GoogleBusinessPlaceEnrichmentMapper
{
    public static BusinessPlaceEnrichment Map(string providerRef, JsonElement place, DateTimeOffset observedAt)
    {
        var channels = new List<string>(3);
        AddWhenTrue(place, "dineIn", "Dine in", channels);
        AddWhenTrue(place, "takeout", "Takeaway", channels);
        AddWhenTrue(place, "delivery", "Delivery", channels);

        var servicePeriods = new List<string>(4);
        AddWhenTrue(place, "servesBreakfast", "Breakfast", servicePeriods);
        AddWhenTrue(place, "servesBrunch", "Brunch", servicePeriods);
        AddWhenTrue(place, "servesLunch", "Lunch", servicePeriods);
        AddWhenTrue(place, "servesDinner", "Dinner", servicePeriods);

        return new BusinessPlaceEnrichment(
            providerRef,
            observedAt,
            channels,
            NullableBoolean(place, "reservable"),
            servicePeriods,
            PricePosition(place),
            OpeningHours(place),
            Attributions(place));
    }

    private static void AddWhenTrue(JsonElement element, string property, string label, ICollection<string> target)
    {
        if (NullableBoolean(element, property) is true) target.Add(label);
    }

    private static bool? NullableBoolean(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static string? PricePosition(JsonElement place)
    {
        if (!place.TryGetProperty("priceLevel", out var value) || value.ValueKind != JsonValueKind.String)
            return null;

        return value.GetString() switch
        {
            "PRICE_LEVEL_FREE" => "Free",
            "PRICE_LEVEL_INEXPENSIVE" => "Inexpensive",
            "PRICE_LEVEL_MODERATE" => "Moderate",
            "PRICE_LEVEL_EXPENSIVE" => "Expensive",
            "PRICE_LEVEL_VERY_EXPENSIVE" => "Very expensive",
            _ => null
        };
    }

    private static IReadOnlyList<string> OpeningHours(JsonElement place)
    {
        if (!place.TryGetProperty("regularOpeningHours", out var hours) ||
            hours.ValueKind != JsonValueKind.Object ||
            !hours.TryGetProperty("weekdayDescriptions", out var descriptions) ||
            descriptions.ValueKind != JsonValueKind.Array)
            return [];

        return descriptions.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()?.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .Take(7)
            .ToList();
    }

    private static IReadOnlyList<BusinessPlaceAttribution> Attributions(JsonElement place)
    {
        if (!place.TryGetProperty("attributions", out var attributions) || attributions.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<BusinessPlaceAttribution>();
        foreach (var attribution in attributions.EnumerateArray())
        {
            if (attribution.ValueKind != JsonValueKind.Object) continue;
            var provider = String(attribution, "provider");
            if (string.IsNullOrWhiteSpace(provider)) continue;
            result.Add(new BusinessPlaceAttribution(provider, String(attribution, "providerUri")));
        }
        return result;
    }

    private static string? String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;
}
