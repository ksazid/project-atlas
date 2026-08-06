using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class KnowledgePackEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgePackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/businesses/{businessId:guid}/knowledge-pack", async (
            Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();

            var assignment = await db.BusinessKnowledgeAssignments
                .AsNoTracking()
                .Include(x => x.KnowledgePack)
                .Include(x => x.KnowledgePackVersion)
                    .ThenInclude(x => x.Sections)
                .Where(x => x.BusinessId == businessId && x.IsCurrent &&
                    db.BusinessMemberships.Any(m => m.BusinessId == x.BusinessId &&
                        m.UserAccount.ProviderSubject == subject && m.Role == MembershipRoles.BusinessOwner))
                .SingleOrDefaultAsync(ct);

            if (assignment is null) return Results.NotFound();
            return Results.Ok(ToResponse(assignment));
        }).RequireAuthorization("BusinessOwner");

        endpoints.MapGet("/api/v1/businesses/{businessId:guid}/knowledge-pack/versions/{versionId:guid}", async (
            Guid businessId, Guid versionId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();

            var assignment = await db.BusinessKnowledgeAssignments
                .AsNoTracking()
                .Include(x => x.KnowledgePack)
                .Include(x => x.KnowledgePackVersion)
                    .ThenInclude(x => x.Sections)
                .Where(x => x.BusinessId == businessId && x.KnowledgePackVersionId == versionId &&
                    db.BusinessMemberships.Any(m => m.BusinessId == x.BusinessId &&
                        m.UserAccount.ProviderSubject == subject && m.Role == MembershipRoles.BusinessOwner))
                .OrderByDescending(x => x.AssignedAt)
                .FirstOrDefaultAsync(ct);

            return assignment is null ? Results.NotFound() : Results.Ok(ToResponse(assignment));
        }).RequireAuthorization("BusinessOwner");

        return endpoints;
    }

    private static KnowledgePackResponse ToResponse(BusinessKnowledgeAssignment assignment) => new(
        assignment.PackKey,
        assignment.KnowledgePack.Name,
        assignment.KnowledgePack.Description,
        assignment.ExactVersion,
        assignment.KnowledgePackVersion.Status,
        assignment.KnowledgePackVersion.Locale,
        assignment.KnowledgePackVersion.Sections
            .OrderBy(x => x.Order)
            .Select(x => new KnowledgeSectionResponse(x.Id, x.StableKey, x.Category, x.Title, x.Content, x.MetadataJson, x.Order, x.Locale))
            .ToList(),
        assignment.AssignedAt);
}
