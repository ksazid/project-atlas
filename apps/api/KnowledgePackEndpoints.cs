using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class KnowledgePackEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgePackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/businesses/{businessId:guid}/knowledge-packs", async (
            Guid businessId,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();

            var ownsBusiness = await db.BusinessMemberships.AnyAsync(
                x => x.BusinessId == businessId &&
                     x.UserAccount.ProviderSubject == subject &&
                     x.Role == MembershipRoles.BusinessOwner,
                ct);
            if (!ownsBusiness) return Results.NotFound();

            var assignments = await db.Set<BusinessKnowledgePack>()
                .AsNoTracking()
                .Include(x => x.KnowledgePack)
                .Where(x => x.BusinessId == businessId && x.IsActive)
                .OrderBy(x => x.PackKey)
                .ThenBy(x => x.PackVersion)
                .ToListAsync(ct);

            return Results.Ok(assignments.Select(KnowledgePackResponse.From));
        }).RequireAuthorization("BusinessOwner");

        endpoints.MapGet("/api/v1/businesses/{businessId:guid}/knowledge-packs/{key}/{version}", async (
            Guid businessId,
            string key,
            string version,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            if (!KnowledgePackKeys.IsValid(key) || !KnowledgePackVersions.IsValid(version)) return Results.NotFound();

            var assignment = await db.Set<BusinessKnowledgePack>()
                .AsNoTracking()
                .Include(x => x.KnowledgePack)
                .Where(x => x.BusinessId == businessId &&
                            x.IsActive &&
                            x.PackKey == key &&
                            x.PackVersion == version &&
                            db.BusinessMemberships.Any(m =>
                                m.BusinessId == x.BusinessId &&
                                m.UserAccount.ProviderSubject == subject &&
                                m.Role == MembershipRoles.BusinessOwner))
                .SingleOrDefaultAsync(ct);

            return assignment is null ? Results.NotFound() : Results.Ok(KnowledgePackResponse.From(assignment));
        }).RequireAuthorization("BusinessOwner");

        return endpoints;
    }
}
