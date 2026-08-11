using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed record EnrichBusinessPlaceRequest(string? ProviderRef);

public sealed record BusinessPlaceEnrichmentResponse(
    string ProviderRef,
    IReadOnlyList<string> OperatingChannels,
    bool? Reservable,
    IReadOnlyList<string> ServicePeriods,
    string? PricePosition,
    IReadOnlyList<string> OpeningHours,
    IReadOnlyList<BusinessPlaceAttribution> Attributions,
    string AttributionLabel);

public sealed class BusinessPlaceEnrichmentException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public static class BusinessPlaceEnrichmentService
{
    public static async Task<BusinessPlaceEnrichmentResponse> GetAsync(
        AtlasDbContext db,
        string subject,
        Guid snapshotId,
        string providerRef,
        IBusinessPlaceEnrichmentProvider provider,
        CancellationToken ct)
    {
        var normalizedSubject = subject.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSubject))
            throw new BusinessPlaceEnrichmentException(
                "business_place_enrichment_not_found",
                "Business place enrichment is unavailable for this session.");

        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.ProviderSubject == normalizedSubject, ct);
        if (account is null)
            throw new BusinessPlaceEnrichmentException(
                "business_place_enrichment_not_found",
                "Business place enrichment is unavailable for this Business.");

        var snapshot = await db.BusinessDiscoverySnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == snapshotId && x.UserAccountId == account.Id, ct);
        if (snapshot is null)
            throw new BusinessPlaceEnrichmentException(
                "business_place_enrichment_not_found",
                "Business place enrichment is unavailable for this Business.");

        var placeId = providerRef.Trim();
        if (placeId.Length is < 1 or > 2048)
            throw new BusinessPlaceEnrichmentException(
                "business_place_ref_invalid",
                "Choose a valid business location before requesting more details.");

        BusinessPlaceEnrichment? enrichment;
        try
        {
            enrichment = await provider.GetAsync(placeId, ct);
        }
        catch (HttpRequestException)
        {
            enrichment = null;
        }
        catch (JsonException)
        {
            enrichment = null;
        }

        if (enrichment is null)
            throw new BusinessPlaceEnrichmentException(
                "business_place_enrichment_unavailable",
                "Atlas cannot load extra operating details right now.");

        return new BusinessPlaceEnrichmentResponse(
            enrichment.ProviderRef,
            enrichment.OperatingChannels,
            enrichment.Reservable,
            enrichment.ServicePeriods,
            enrichment.PricePosition,
            enrichment.OpeningHours,
            enrichment.Attributions,
            "Google Maps");
    }
}

public static class BusinessPlaceEnrichmentEndpoints
{
    public static IEndpointRouteBuilder MapBusinessPlaceEnrichmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/business-discovery/{snapshotId:guid}/place-enrichment", async (
            Guid snapshotId,
            EnrichBusinessPlaceRequest request,
            ClaimsPrincipal user,
            AtlasDbContext db,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            var subject = Subject(user);
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();

            try
            {
                var provider = new GoogleBusinessPlaceEnrichmentProvider(httpClientFactory.CreateClient(), configuration);
                var result = await BusinessPlaceEnrichmentService.GetAsync(
                    db,
                    subject,
                    snapshotId,
                    request.ProviderRef ?? string.Empty,
                    provider,
                    ct);
                return Results.Ok(result);
            }
            catch (BusinessPlaceEnrichmentException ex) when (ex.Code == "business_place_enrichment_not_found")
            {
                return Results.NotFound();
            }
            catch (BusinessPlaceEnrichmentException ex) when (ex.Code == "business_place_ref_invalid")
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { [nameof(request.ProviderRef)] = [ex.Message] },
                    extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
            }
            catch (BusinessPlaceEnrichmentException ex) when (ex.Code == "business_place_enrichment_unavailable")
            {
                return Results.Problem(
                    statusCode: 503,
                    title: "Business details unavailable",
                    detail: ex.Message,
                    extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
            }
        }).RequireAuthorization("BusinessOwner");

        return app;
    }

    private static string? Subject(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
}
