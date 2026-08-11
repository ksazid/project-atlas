using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed record BusinessHubResponse(
    BusinessResponse Business,
    BusinessHubProfileSummary? Profile,
    IReadOnlyList<BusinessHubMediaItem> Media,
    BusinessHubMenuSummary Menu,
    BusinessHubContextSummary Context,
    DateTimeOffset? LatestObservedAt);

public sealed record BusinessHubProfileSummary(
    string? Description,
    string? Address,
    string? Website,
    string? Phone,
    string? BusinessHours,
    string Source,
    bool OwnerConfirmed,
    DateTimeOffset UpdatedAt);

public sealed record BusinessHubMediaItem(
    string Kind,
    string RemoteUrl,
    string Source,
    string SourceUrl,
    DateTimeOffset ObservedAt,
    string Confidence,
    string EvidenceClass,
    bool OwnerConfirmed,
    string? AltText);

public sealed record BusinessHubMenuPreviewItem(
    string? Section,
    string Name,
    string? Description,
    decimal? Price,
    string? Currency,
    string Source,
    DateTimeOffset ObservedAt);

public sealed record BusinessHubMenuSummary(
    int SectionCount,
    int ItemCount,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Currency,
    IReadOnlyList<BusinessHubMenuPreviewItem> Preview,
    string? Source,
    DateTimeOffset? ObservedAt);

public sealed record BusinessHubContextSummary(int EntryCount, int OwnerConfirmedCount, string Status);

public static class BusinessHubReader
{
    public static async Task<BusinessHubResponse?> BuildAsync(
        AtlasDbContext db,
        Guid businessId,
        string providerSubject,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerSubject)) return null;

        var business = await db.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId && db.BusinessMemberships.Any(m =>
                m.BusinessId == b.Id &&
                m.UserAccount.ProviderSubject == providerSubject &&
                m.Role == MembershipRoles.BusinessOwner))
            .SingleOrDefaultAsync(ct);
        if (business is null) return null;

        var profile = await db.BusinessProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);

        var media = await db.Set<BusinessMediaReference>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Kind == "business-image")
            .OrderBy(x => x.SourceOrder)
            .ThenByDescending(x => x.ObservedAt)
            .ToListAsync(ct);

        var offerings = await db.Set<BusinessOffering>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Kind == "menu-item")
            .OrderBy(x => x.SourceOrder)
            .ThenBy(x => x.Section)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        var context = await db.BusinessContextEntries
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .ToListAsync(ct);

        var safeMedia = media
            .Where(IsSafeHttpsMedia)
            .GroupBy(x => x.RemoteUrl, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(6)
            .Select(x => new BusinessHubMediaItem(
                x.Kind,
                x.RemoteUrl,
                x.Source,
                x.SourceUrl,
                x.ObservedAt,
                x.Confidence,
                x.EvidenceClass,
                x.OwnerConfirmed,
                x.AltText))
            .ToList();

        var pricedOfferings = offerings
            .Where(x => x.Price is not null && !string.IsNullOrWhiteSpace(x.Currency))
            .ToList();
        var currencies = pricedOfferings
            .Select(x => x.Currency!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var singleCurrency = currencies.Count == 1 ? currencies[0].ToUpperInvariant() : null;
        var prices = singleCurrency is null
            ? []
            : pricedOfferings.Select(x => x.Price!.Value).ToList();
        var minPrice = prices.Count == 0 ? null : prices.Min();
        var maxPrice = prices.Count == 0 ? null : prices.Max();

        var preview = offerings
            .Take(5)
            .Select(x => new BusinessHubMenuPreviewItem(
                x.Section,
                x.Name,
                x.Description,
                x.Price,
                x.Currency,
                x.Source,
                x.ObservedAt))
            .ToList();

        var menuObservedAt = offerings
            .Select(x => (DateTimeOffset?)x.ObservedAt)
            .Max();
        var latestObservedAt = media
            .Select(x => (DateTimeOffset?)x.ObservedAt)
            .Concat(offerings.Select(x => (DateTimeOffset?)x.ObservedAt))
            .Max();
        var contextStatus = context.Count >= 5 ? "strong" : context.Count >= 2 ? "partial" : "sparse";

        return new BusinessHubResponse(
            BusinessResponse.From(business),
            profile is null
                ? null
                : new BusinessHubProfileSummary(
                    profile.Description,
                    profile.Address,
                    profile.Website,
                    profile.Phone,
                    profile.BusinessHours,
                    profile.Source,
                    profile.OwnerConfirmed,
                    profile.UpdatedAt),
            safeMedia,
            new BusinessHubMenuSummary(
                offerings
                    .Select(x => x.Section)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                offerings.Count,
                minPrice,
                maxPrice,
                singleCurrency,
                preview,
                offerings.FirstOrDefault()?.Source,
                menuObservedAt),
            new BusinessHubContextSummary(
                context.Count,
                context.Count(x => x.OwnerConfirmed),
                contextStatus),
            latestObservedAt);
    }

    private static bool IsSafeHttpsMedia(BusinessMediaReference item) =>
        Uri.TryCreate(item.RemoteUrl, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}

public static class BusinessHubEndpoints
{
    public static void MapBusinessHubEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/businesses/{businessId:guid}/hub", async (
            Guid businessId,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(subject)) return Results.NotFound();

            var hub = await BusinessHubReader.BuildAsync(db, businessId, subject, ct);
            return hub is null ? Results.NotFound() : Results.Ok(hub);
        }).RequireAuthorization("BusinessOwner");
    }
}
