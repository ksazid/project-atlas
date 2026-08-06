using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed record CreateKnowledgePackRequest(string Key, string Name, string Description, string Locale);
public sealed record CreateKnowledgePackVersionRequest(string VersionNumber, string Locale, Guid? SourceVersionId);
public sealed record UpsertKnowledgeSectionRequest(string StableKey, string Category, string Title, string Content, string? MetadataJson, int Order, string Locale, string? TranslationGroupKey, string? Source, uint? ExpectedVersion);
public sealed record TransitionKnowledgePackVersionRequest(string Status, uint? ExpectedVersion);
public sealed record AssignKnowledgePackVersionRequest(Guid KnowledgePackVersionId, uint? ExpectedCurrentAssignmentVersion);
public sealed record KnowledgePackVersionSummary(Guid Id, string VersionNumber, string Status, string Locale, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt);
public sealed record KnowledgePackSummary(Guid Id, string Key, string Name, string Description, bool IsArchived, IReadOnlyList<KnowledgePackVersionSummary> Versions);
public sealed record AssignmentHistoryResponse(Guid Id, Guid KnowledgePackVersionId, string PackKey, string ExactVersion, bool IsCurrent, DateTimeOffset AssignedAt, DateTimeOffset EffectiveAt, DateTimeOffset? EndedAt);
public sealed record VersionComparisonResponse(Guid LeftVersionId, Guid RightVersionId, IReadOnlyList<string> AddedSections, IReadOnlyList<string> RemovedSections, IReadOnlyList<string> ChangedSections, IReadOnlyList<string> ReorderedSections);

public static class KnowledgePackEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgePackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapBusinessReadEndpoints(endpoints);
        MapAdminManagementEndpoints(endpoints);
        return endpoints;
    }

    private static void MapBusinessReadEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/businesses/{businessId:guid}/knowledge-pack", async (
            Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            if (!await OwnsBusiness(businessId, user, db, ct)) return Results.NotFound();

            var assignment = await db.BusinessKnowledgeAssignments
                .AsNoTracking()
                .Include(x => x.KnowledgePack)
                .Include(x => x.KnowledgePackVersion).ThenInclude(x => x.Sections)
                .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.IsCurrent, ct);

            return assignment is null ? Results.NotFound() : Results.Ok(ToResponse(assignment));
        }).RequireAuthorization("BusinessOwner");

        endpoints.MapGet("/api/v1/businesses/{businessId:guid}/knowledge-pack/versions/{versionId:guid}", async (
            Guid businessId, Guid versionId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            if (!await OwnsBusiness(businessId, user, db, ct)) return Results.NotFound();

            var assignment = await db.BusinessKnowledgeAssignments
                .AsNoTracking()
                .Include(x => x.KnowledgePack)
                .Include(x => x.KnowledgePackVersion).ThenInclude(x => x.Sections)
                .Where(x => x.BusinessId == businessId && x.KnowledgePackVersionId == versionId)
                .OrderByDescending(x => x.AssignedAt)
                .FirstOrDefaultAsync(ct);

            return assignment is null ? Results.NotFound() : Results.Ok(ToResponse(assignment));
        }).RequireAuthorization("BusinessOwner");

        endpoints.MapGet("/api/v1/businesses/{businessId:guid}/knowledge-pack/assignments", async (
            Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            if (!await OwnsBusiness(businessId, user, db, ct)) return Results.NotFound();
            var history = await db.BusinessKnowledgeAssignments.AsNoTracking()
                .Where(x => x.BusinessId == businessId)
                .OrderByDescending(x => x.AssignedAt)
                .Select(x => new AssignmentHistoryResponse(x.Id, x.KnowledgePackVersionId, x.PackKey, x.ExactVersion, x.IsCurrent, x.AssignedAt, x.EffectiveAt, x.EndedAt))
                .ToListAsync(ct);
            return Results.Ok(history);
        }).RequireAuthorization("BusinessOwner");
    }

    private static void MapAdminManagementEndpoints(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/v1/admin/knowledge-packs").RequireAuthorization("InternalOperator");

        admin.MapGet("/", async (AtlasDbContext db, CancellationToken ct) =>
        {
            var packs = await db.KnowledgePacks.AsNoTracking()
                .Include(x => x.Versions)
                .OrderBy(x => x.Name)
                .ToListAsync(ct);
            return Results.Ok(packs.Select(x => new KnowledgePackSummary(
                x.Id, x.Key, x.Name, x.Description, x.IsArchived,
                x.Versions.OrderByDescending(v => v.CreatedAt)
                    .Select(v => new KnowledgePackVersionSummary(v.Id, v.VersionNumber, v.Status, v.Locale, v.CreatedAt, v.PublishedAt)).ToList())));
        });

        admin.MapPost("/", async (CreateKnowledgePackRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var actor = await ResolveAccount(user, db, ct);
            if (actor is null) return Results.Unauthorized();
            if (!KnowledgePackKeys.IsValid(request.Key) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Locale))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["knowledgePack"] = ["Key, name, description and locale are required."] }, extensions: new Dictionary<string, object?> { ["code"] = "knowledge_pack_invalid" });
            if (await db.KnowledgePacks.AnyAsync(x => x.Key == request.Key, ct))
                return Results.Conflict(new { code = "knowledge_pack_key_exists" });

            var pack = new KnowledgePack
            {
                Id = Guid.NewGuid(), Key = request.Key.Trim(), Name = request.Name.Trim(), Description = request.Description.Trim(),
                CreatedByUserAccountId = actor.Id, CreatedAt = DateTimeOffset.UtcNow
            };
            var version = new KnowledgePackVersion
            {
                Id = Guid.NewGuid(), KnowledgePackId = pack.Id, KnowledgePack = pack, VersionNumber = "1.0",
                Status = KnowledgePackStatuses.Draft, Locale = request.Locale.Trim(), CreatedByUserAccountId = actor.Id, CreatedAt = DateTimeOffset.UtcNow
            };
            pack.Versions.Add(version);
            db.KnowledgePacks.Add(pack);
            db.AuditRecords.Add(AuditRecord.Create(actor.Id, null, $"knowledge-pack.created:{pack.Key}"));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/admin/knowledge-packs/{pack.Id}", new { pack.Id, versionId = version.Id });
        });

        admin.MapPost("/{packId:guid}/versions", async (Guid packId, CreateKnowledgePackVersionRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var actor = await ResolveAccount(user, db, ct);
            if (actor is null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.VersionNumber) || string.IsNullOrWhiteSpace(request.Locale))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["version"] = ["Version number and locale are required."] });

            var pack = await db.KnowledgePacks.SingleOrDefaultAsync(x => x.Id == packId && !x.IsArchived, ct);
            if (pack is null) return Results.NotFound();
            if (await db.KnowledgePackVersions.AnyAsync(x => x.KnowledgePackId == packId && x.VersionNumber == request.VersionNumber, ct))
                return Results.Conflict(new { code = "knowledge_pack_version_exists" });

            var version = new KnowledgePackVersion
            {
                Id = Guid.NewGuid(), KnowledgePackId = packId, KnowledgePack = pack, VersionNumber = request.VersionNumber.Trim(),
                Status = KnowledgePackStatuses.Draft, Locale = request.Locale.Trim(), CreatedByUserAccountId = actor.Id, CreatedAt = DateTimeOffset.UtcNow
            };
            if (request.SourceVersionId is Guid sourceId)
            {
                var source = await db.KnowledgePackVersions.AsNoTracking().Include(x => x.Sections)
                    .SingleOrDefaultAsync(x => x.Id == sourceId && x.KnowledgePackId == packId && x.Status == KnowledgePackStatuses.Published, ct);
                if (source is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["sourceVersionId"] = ["Source must be a published version of this Knowledge Pack."] });
                version.Sections = source.Sections.OrderBy(x => x.Order).Select(x => new KnowledgeSection
                {
                    Id = Guid.NewGuid(), KnowledgePackVersionId = version.Id, KnowledgePackVersion = version, StableKey = x.StableKey,
                    Category = x.Category, Title = x.Title, Content = x.Content, MetadataJson = x.MetadataJson, Order = x.Order,
                    Locale = x.Locale, TranslationGroupKey = x.TranslationGroupKey, Source = x.Source,
                    CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                }).ToList();
            }
            db.KnowledgePackVersions.Add(version);
            db.AuditRecords.Add(AuditRecord.Create(actor.Id, null, $"knowledge-pack.version.created:{pack.Key}:{version.VersionNumber}"));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/admin/knowledge-packs/{packId}/versions/{version.Id}", new { version.Id });
        });

        admin.MapPut("/{packId:guid}/versions/{versionId:guid}/sections/{sectionId:guid}", async (
            Guid packId, Guid versionId, Guid sectionId, UpsertKnowledgeSectionRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var actor = await ResolveAccount(user, db, ct);
            if (actor is null) return Results.Unauthorized();
            var version = await db.KnowledgePackVersions.Include(x => x.Sections)
                .SingleOrDefaultAsync(x => x.Id == versionId && x.KnowledgePackId == packId, ct);
            if (version is null) return Results.NotFound();
            try { version.EnsureEditable(); } catch (InvalidOperationException ex) { return Results.Conflict(new { code = "knowledge_pack_version_immutable", message = ex.Message }); }
            if (string.IsNullOrWhiteSpace(request.StableKey) || string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.Locale) || request.Order < 1)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["section"] = ["Stable key, category, title, content, locale and a positive order are required."] });
            if (version.Sections.Any(x => x.Id != sectionId && (x.StableKey == request.StableKey || x.Order == request.Order)))
                return Results.Conflict(new { code = "knowledge_section_duplicate_key_or_order" });

            var section = version.Sections.SingleOrDefault(x => x.Id == sectionId);
            if (section is null)
            {
                section = new KnowledgeSection
                {
                    Id = sectionId,
                    KnowledgePackVersionId = versionId,
                    KnowledgePackVersion = version,
                    StableKey = request.StableKey.Trim(),
                    Category = request.Category.Trim(),
                    Title = request.Title.Trim(),
                    Content = request.Content.Trim(),
                    MetadataJson = request.MetadataJson,
                    Order = request.Order,
                    Locale = request.Locale.Trim(),
                    TranslationGroupKey = request.TranslationGroupKey?.Trim(),
                    Source = request.Source?.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                version.Sections.Add(section);
            }
            else
            {
                if (request.ExpectedVersion is uint expected)
                    db.Entry(section).Property(x => x.ConcurrencyVersion).OriginalValue = expected;
                section.StableKey = request.StableKey.Trim();
                section.Category = request.Category.Trim();
                section.Title = request.Title.Trim();
                section.Content = request.Content.Trim();
                section.MetadataJson = request.MetadataJson;
                section.Order = request.Order;
                section.Locale = request.Locale.Trim();
                section.TranslationGroupKey = request.TranslationGroupKey?.Trim();
                section.Source = request.Source?.Trim();
                section.UpdatedAt = DateTimeOffset.UtcNow;
            }
            db.AuditRecords.Add(AuditRecord.Create(actor.Id, null, $"knowledge-pack.section.upserted:{packId}:{versionId}:{sectionId}"));
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict(new { code = "knowledge_section_stale" }); }
            return Results.Ok(new KnowledgeSectionResponse(section.Id, section.StableKey, section.Category, section.Title, section.Content, section.MetadataJson, section.Order, section.Locale));
        });

        admin.MapDelete("/{packId:guid}/versions/{versionId:guid}/sections/{sectionId:guid}", async (
            Guid packId, Guid versionId, Guid sectionId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var actor = await ResolveAccount(user, db, ct);
            if (actor is null) return Results.Unauthorized();
            var version = await db.KnowledgePackVersions.Include(x => x.Sections)
                .SingleOrDefaultAsync(x => x.Id == versionId && x.KnowledgePackId == packId, ct);
            if (version is null) return Results.NotFound();
            try { version.EnsureEditable(); } catch (InvalidOperationException ex) { return Results.Conflict(new { code = "knowledge_pack_version_immutable", message = ex.Message }); }
            var section = version.Sections.SingleOrDefault(x => x.Id == sectionId);
            if (section is null) return Results.NotFound();
            db.KnowledgeSections.Remove(section);
            db.AuditRecords.Add(AuditRecord.Create(actor.Id, null, $"knowledge-pack.section.deleted:{packId}:{versionId}:{sectionId}"));
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        admin.MapPost("/{packId:guid}/versions/{versionId:guid}/transition", async (
            Guid packId, Guid versionId, TransitionKnowledgePackVersionRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var actor = await ResolveAccount(user, db, ct);
            if (actor is null) return Results.Unauthorized();
            var version = await db.KnowledgePackVersions.Include(x => x.Sections)
                .SingleOrDefaultAsync(x => x.Id == versionId && x.KnowledgePackId == packId, ct);
            if (version is null) return Results.NotFound();
            if (request.ExpectedVersion is uint expected) db.Entry(version).Property(x => x.ConcurrencyVersion).OriginalValue = expected;
            try { version.TransitionTo(request.Status, actor.Id); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { code = "knowledge_pack_transition_invalid", message = ex.Message }); }
            db.AuditRecords.Add(AuditRecord.Create(actor.Id, null, $"knowledge-pack.version.transitioned:{packId}:{versionId}:{request.Status}"));
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict(new { code = "knowledge_pack_version_stale" }); }
            return Results.Ok(new KnowledgePackVersionSummary(version.Id, version.VersionNumber, version.Status, version.Locale, version.CreatedAt, version.PublishedAt));
        });

        admin.MapPost("/businesses/{businessId:guid}/assignment", async (
            Guid businessId, AssignKnowledgePackVersionRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var actor = await ResolveAccount(user, db, ct);
            if (actor is null) return Results.Unauthorized();
            if (!await db.Businesses.AnyAsync(x => x.Id == businessId, ct)) return Results.NotFound();
            var version = await db.KnowledgePackVersions.Include(x => x.KnowledgePack)
                .SingleOrDefaultAsync(x => x.Id == request.KnowledgePackVersionId, ct);
            if (version is null || version.Status != KnowledgePackStatuses.Published)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["knowledgePackVersionId"] = ["Assignment requires a published Knowledge Pack version."] });

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var current = await db.BusinessKnowledgeAssignments.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.IsCurrent, ct);
            if (current is not null)
            {
                if (request.ExpectedCurrentAssignmentVersion is uint expected) db.Entry(current).Property(x => x.ConcurrencyVersion).OriginalValue = expected;
                if (current.KnowledgePackVersionId == version.Id) return Results.Ok(new { assignmentId = current.Id, unchanged = true });
                current.IsCurrent = false;
                current.EndedAt = DateTimeOffset.UtcNow;
            }
            var assignment = BusinessKnowledgeAssignment.Assign(businessId, version.KnowledgePack, version, actor.Id);
            db.BusinessKnowledgeAssignments.Add(assignment);
            db.AuditRecords.Add(AuditRecord.Create(actor.Id, businessId, $"knowledge-pack.assigned:{assignment.PackKey}:{assignment.ExactVersion}"));
            try
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateConcurrencyException) { return Results.Conflict(new { code = "knowledge_assignment_stale" }); }
            return Results.Ok(new { assignmentId = assignment.Id, assignment.ExactVersion });
        });

        admin.MapGet("/{packId:guid}/compare", async (Guid packId, Guid leftVersionId, Guid rightVersionId, AtlasDbContext db, CancellationToken ct) =>
        {
            var versions = await db.KnowledgePackVersions.AsNoTracking().Include(x => x.Sections)
                .Where(x => x.KnowledgePackId == packId && (x.Id == leftVersionId || x.Id == rightVersionId))
                .ToListAsync(ct);
            var left = versions.SingleOrDefault(x => x.Id == leftVersionId);
            var right = versions.SingleOrDefault(x => x.Id == rightVersionId);
            if (left is null || right is null) return Results.NotFound();
            var leftByKey = left.Sections.ToDictionary(x => x.StableKey);
            var rightByKey = right.Sections.ToDictionary(x => x.StableKey);
            var added = rightByKey.Keys.Except(leftByKey.Keys).Order().ToList();
            var removed = leftByKey.Keys.Except(rightByKey.Keys).Order().ToList();
            var common = leftByKey.Keys.Intersect(rightByKey.Keys).ToList();
            var changed = common.Where(k => leftByKey[k].Title != rightByKey[k].Title || leftByKey[k].Content != rightByKey[k].Content || leftByKey[k].MetadataJson != rightByKey[k].MetadataJson).Order().ToList();
            var reordered = common.Where(k => leftByKey[k].Order != rightByKey[k].Order).Order().ToList();
            return Results.Ok(new VersionComparisonResponse(left.Id, right.Id, added, removed, changed, reordered));
        });
    }

    private static async Task<UserAccount?> ResolveAccount(ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return string.IsNullOrWhiteSpace(subject) ? null : await db.UserAccounts.SingleOrDefaultAsync(x => x.ProviderSubject == subject, ct);
    }

    private static async Task<bool> OwnsBusiness(Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return !string.IsNullOrWhiteSpace(subject) && await db.BusinessMemberships.AnyAsync(x =>
            x.BusinessId == businessId && x.UserAccount.ProviderSubject == subject && x.Role == MembershipRoles.BusinessOwner, ct);
    }

    private static KnowledgePackResponse ToResponse(BusinessKnowledgeAssignment assignment) => new(
        assignment.PackKey, assignment.KnowledgePack.Name, assignment.KnowledgePack.Description,
        assignment.ExactVersion, assignment.KnowledgePackVersion.Status, assignment.KnowledgePackVersion.Locale,
        assignment.KnowledgePackVersion.Sections.OrderBy(x => x.Order)
            .Select(x => new KnowledgeSectionResponse(x.Id, x.StableKey, x.Category, x.Title, x.Content, x.MetadataJson, x.Order, x.Locale)).ToList(),
        assignment.AssignedAt);
}
