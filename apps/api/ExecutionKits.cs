using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class ExecutionAssetTypes
{
    public const string Checklist = "checklist";
    public const string MessageTemplate = "message-template";
    public const string MeasurementSuggestion = "measurement-suggestion";
    public static bool IsSupported(string value) => value is Checklist or MessageTemplate or MeasurementSuggestion;
}

public sealed class ExecutionKit
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OpportunityId { get; set; }
    public Guid? GoalId { get; set; }
    public required string KnowledgePackKey { get; set; }
    public required string KnowledgePackVersion { get; set; }
    public int VersionNumber { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint ConcurrencyVersion { get; set; }
    public ICollection<ExecutionAsset> Assets { get; set; } = [];
}

public sealed class ExecutionAsset
{
    public Guid Id { get; set; }
    public Guid ExecutionKitId { get; set; }
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public bool IsEditable { get; set; }
    public bool IsUsed { get; set; }
    public int CopyCount { get; set; }
    public int? UsefulnessRating { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint ConcurrencyVersion { get; set; }
    public ExecutionKit ExecutionKit { get; set; } = null!;
}

public sealed record ExecutionAssetResponse(Guid Id, string Type, string Title, string Content, bool IsEditable, bool IsUsed, int CopyCount, int? UsefulnessRating, uint Version);
public sealed record ExecutionKitResponse(Guid Id, Guid OpportunityId, string KnowledgePackKey, string KnowledgePackVersion, int VersionNumber, string Status, IReadOnlyList<ExecutionAssetResponse> Assets, uint Version);
public sealed record UpdateExecutionAssetRequest(string Content, bool IsUsed, int? UsefulnessRating, uint Version);
public sealed record TrackExecutionAssetCopyRequest(uint Version);

public static class ExecutionKitPolicy
{
    public static bool IsEligible(Opportunity opportunity, DateTimeOffset now) =>
        opportunity.BusinessId != Guid.Empty && opportunity.ExpiresAt > now && opportunity.Status is OpportunityStatuses.Available or OpportunityStatuses.Applied;

    public static bool IsValidRating(int? rating) => rating is null or >= 1 and <= 5;
}

public static class ExecutionKitEndpoints
{
    private static string? Subject(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

    private static async Task<UserAccount?> OwnerAccount(Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        var subject = Subject(user);
        if (string.IsNullOrWhiteSpace(subject)) return null;
        var membership = await db.BusinessMemberships.Include(x => x.UserAccount)
            .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.UserAccount.ProviderSubject == subject && x.Role == MembershipRoles.BusinessOwner, ct);
        return membership?.UserAccount;
    }

    private static ExecutionKitResponse ToResponse(ExecutionKit kit) => new(
        kit.Id, kit.OpportunityId, kit.KnowledgePackKey, kit.KnowledgePackVersion, kit.VersionNumber, kit.Status,
        kit.Assets.OrderBy(x => x.Title).Select(x => new ExecutionAssetResponse(x.Id, x.Type, x.Title, x.Content, x.IsEditable, x.IsUsed, x.CopyCount, x.UsefulnessRating, x.ConcurrencyVersion)).ToList(),
        kit.ConcurrencyVersion);

    private static ExecutionKit Create(Opportunity opportunity)
    {
        var now = DateTimeOffset.UtcNow;
        var kit = new ExecutionKit
        {
            Id = Guid.NewGuid(), BusinessId = opportunity.BusinessId, OpportunityId = opportunity.Id, GoalId = opportunity.GoalId,
            KnowledgePackKey = opportunity.KnowledgePackKey, KnowledgePackVersion = opportunity.KnowledgePackVersion,
            VersionNumber = 1, Status = "ready", CreatedAt = now, UpdatedAt = now
        };
        kit.Assets =
        [
            new ExecutionAsset { Id = Guid.NewGuid(), ExecutionKitId = kit.Id, ExecutionKit = kit, Type = ExecutionAssetTypes.Checklist, Title = "Action checklist", Content = "1. Review the proposed action.\n2. Confirm the owner and timing.\n3. Complete the smallest measurable step.\n4. Record what happened.", IsEditable = true, UpdatedAt = now },
            new ExecutionAsset { Id = Guid.NewGuid(), ExecutionKitId = kit.Id, ExecutionKit = kit, Type = ExecutionAssetTypes.MessageTemplate, Title = "Message template", Content = $"We are taking one practical step toward {opportunity.Title}. Please review the plan and share any constraints before we proceed.", IsEditable = true, UpdatedAt = now },
            new ExecutionAsset { Id = Guid.NewGuid(), ExecutionKitId = kit.Id, ExecutionKit = kit, Type = ExecutionAssetTypes.MeasurementSuggestion, Title = "Measurement suggestion", Content = "Choose one observable measure before acting, record the baseline, and compare again after the action. Treat the result as owner-reported unless independently measured.", IsEditable = false, UpdatedAt = now }
        ];
        return kit;
    }

    public static IEndpointRouteBuilder MapExecutionKitEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/businesses/{businessId:guid}/opportunities/{opportunityId:guid}/execution-kit", async (Guid businessId, Guid opportunityId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();
            var opportunity = await db.Set<Opportunity>().SingleOrDefaultAsync(x => x.Id == opportunityId && x.BusinessId == businessId, ct);
            if (opportunity is null) return Results.NotFound();
            if (!ExecutionKitPolicy.IsEligible(opportunity, DateTimeOffset.UtcNow))
                return Results.Conflict(new { code = "execution_kit_unavailable", message = "This Opportunity is no longer eligible for an Execution Kit." });

            var kit = await db.ExecutionKits.Include(x => x.Assets).SingleOrDefaultAsync(x => x.BusinessId == businessId && x.OpportunityId == opportunityId, ct);
            if (kit is null)
            {
                kit = Create(opportunity);
                db.ExecutionKits.Add(kit);
                db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"execution-kit.created:{kit.Id}"));
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok(ToResponse(kit));
        }).RequireAuthorization("BusinessOwner");

        app.MapPut("/api/v1/businesses/{businessId:guid}/execution-kits/{kitId:guid}/assets/{assetId:guid}", async (Guid businessId, Guid kitId, Guid assetId, UpdateExecutionAssetRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();
            if (!ExecutionKitPolicy.IsValidRating(request.UsefulnessRating))
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.UsefulnessRating)] = ["Usefulness rating must be between 1 and 5."] });

            var asset = await db.ExecutionAssets.Include(x => x.ExecutionKit)
                .SingleOrDefaultAsync(x => x.Id == assetId && x.ExecutionKitId == kitId && x.ExecutionKit.BusinessId == businessId, ct);
            if (asset is null) return Results.NotFound();
            if (asset.ConcurrencyVersion != request.Version)
                return Results.Conflict(new { code = "execution_asset_stale", message = "This asset changed. Refresh before saving." });
            if (asset.IsEditable && string.IsNullOrWhiteSpace(request.Content))
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Content)] = ["Editable content cannot be empty."] });

            if (asset.IsEditable) asset.Content = request.Content.Trim();
            asset.IsUsed = request.IsUsed;
            asset.UsefulnessRating = request.UsefulnessRating;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            asset.ExecutionKit.UpdatedAt = asset.UpdatedAt;
            db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"execution-asset.updated:{asset.Id}"));
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(asset.ExecutionKit));
        }).RequireAuthorization("BusinessOwner");

        app.MapPost("/api/v1/businesses/{businessId:guid}/execution-kits/{kitId:guid}/assets/{assetId:guid}/copied", async (Guid businessId, Guid kitId, Guid assetId, TrackExecutionAssetCopyRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();
            var asset = await db.ExecutionAssets.Include(x => x.ExecutionKit)
                .SingleOrDefaultAsync(x => x.Id == assetId && x.ExecutionKitId == kitId && x.ExecutionKit.BusinessId == businessId, ct);
            if (asset is null) return Results.NotFound();
            if (asset.ConcurrencyVersion != request.Version)
                return Results.Conflict(new { code = "execution_asset_stale", message = "This asset changed. Refresh before recording copy." });
            asset.CopyCount++;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"execution-asset.copied:{asset.Id}"));
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(asset.ExecutionKit));
        }).RequireAuthorization("BusinessOwner");

        return app;
    }
}
