using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class OperationalConnectorStates
{
    public const string Connected = "connected";
    public const string ReauthorizationRequired = "reauthorization-required";
    public const string Disconnected = "disconnected";
}

public static class OperationalSyncSchedules
{
    public const string Manual = "manual";
    public const string Daily = "daily";
    public const string EverySixHours = "every-six-hours";
    public static bool IsSupported(string value) => value is Manual or Daily or EverySixHours;
}

public static class OperationalSyncStates
{
    public const string Completed = "completed";
    public const string Busy = "busy";
    public const string NotConnected = "not-connected";
    public const string ReauthorizationRequired = "reauthorization-required";
}

public sealed record ConnectOperationalConnectorRequest(string FolderId, string Schedule);
public sealed record UpdateOperationalScheduleRequest(string Schedule);
public sealed record OperationalConnectorResponse(Guid Id, string FolderName, string Status, string Schedule,
    DateTimeOffset? LastAttemptAt, DateTimeOffset? LastSuccessAt, string? ErrorCode);
public sealed record OperationalSyncResult(string State, int ProcessedFiles, int UnchangedFiles);

public sealed class OperationalConnectorService(AtlasDbContext db, IOperationalFileSource source)
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(10);

    public async Task<OperationalConnectorResponse> ConnectAsync(Guid businessId, string folderId, string schedule,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!OperationalSyncSchedules.IsSupported(schedule)) throw new ArgumentException("Unsupported sync schedule.", nameof(schedule));
        var folder = await source.ValidateFolderAsync(folderId, cancellationToken);
        var connector = await db.OperationalConnectors.SingleOrDefaultAsync(item => item.BusinessId == businessId, cancellationToken);
        if (connector is null)
        {
            connector = new OperationalConnector
            {
                Id = Guid.NewGuid(), BusinessId = businessId, SourceKind = "google-drive",
                FolderId = folder.Id, FolderName = folder.Name, Status = OperationalConnectorStates.Connected,
                Schedule = schedule, CreatedAt = now, UpdatedAt = now
            };
            db.OperationalConnectors.Add(connector);
        }
        else
        {
            connector.FolderId = folder.Id; connector.FolderName = folder.Name;
            connector.Status = OperationalConnectorStates.Connected; connector.Schedule = schedule;
            connector.ErrorCode = null; connector.UpdatedAt = now;
        }
        await db.SaveChangesAsync(cancellationToken);
        return Response(connector);
    }

    public async Task<OperationalSyncResult> SyncBusinessAsync(Guid businessId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connector = await db.OperationalConnectors.SingleOrDefaultAsync(item => item.BusinessId == businessId, cancellationToken);
        if (connector is null || connector.Status == OperationalConnectorStates.Disconnected)
            return new(OperationalSyncStates.NotConnected, 0, 0);
        if (connector.LeaseUntil is not null && connector.LeaseUntil > now)
            return new(OperationalSyncStates.Busy, 0, 0);

        connector.LeaseUntil = now.Add(LeaseDuration); connector.LastAttemptAt = now; connector.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            var files = await source.ListAsync(connector.FolderId, cancellationToken);
            var processed = 0; var unchanged = 0;
            foreach (var file in files)
            {
                var checkpoint = await db.OperationalFileCheckpoints.SingleOrDefaultAsync(item =>
                    item.BusinessId == businessId && item.ProviderFileId == file.Id, cancellationToken);
                if (checkpoint is not null && checkpoint.ProviderModifiedAt == file.ModifiedAt &&
                    checkpoint.Size == file.Size && file.ProviderChecksum is not null &&
                    checkpoint.ContentFingerprint == file.ProviderChecksum)
                {
                    unchanged++; continue;
                }

                await using var stream = await source.OpenReadAsync(file.Id, cancellationToken);
                var business = await db.Businesses.SingleAsync(item => item.Id == businessId, cancellationToken);
                var normalized = await OperationalCsvReader.NormalizeAsync(stream, business, cancellationToken);
                var ingestion = new OperationalIngestionService(db);
                var result = await ingestion.IngestAsync(businessId, "google-drive", file.Id,
                    normalized.Preview.Fingerprint, normalized, now, cancellationToken);
                if (result.State == OperationalIngestionStates.OverlapConflict) continue;
                if (checkpoint is null)
                {
                    checkpoint = new OperationalFileCheckpoint
                    {
                        Id = Guid.NewGuid(), BusinessId = businessId, ConnectorId = connector.Id,
                        ProviderFileId = file.Id, FileName = file.Name, MimeType = file.MimeType,
                        ContentFingerprint = "", ProcessedAt = now
                    };
                    db.OperationalFileCheckpoints.Add(checkpoint);
                }
                checkpoint.FileName = file.Name; checkpoint.MimeType = file.MimeType; checkpoint.Size = file.Size;
                checkpoint.ProviderModifiedAt = file.ModifiedAt;
                checkpoint.ContentFingerprint = file.ProviderChecksum ?? normalized.Preview.Fingerprint;
                checkpoint.ProcessedAt = now; processed++;
            }
            connector.LastSuccessAt = now; connector.Status = OperationalConnectorStates.Connected; connector.ErrorCode = null;
            await db.SaveChangesAsync(cancellationToken);
            return new(OperationalSyncStates.Completed, processed, unchanged);
        }
        catch (GoogleDriveOperationalException error) when (error.Code == GoogleDriveOperationalErrorCodes.ReauthorizationRequired)
        {
            connector.Status = OperationalConnectorStates.ReauthorizationRequired; connector.ErrorCode = error.Code;
            await db.SaveChangesAsync(cancellationToken);
            return new(OperationalSyncStates.ReauthorizationRequired, 0, 0);
        }
        finally
        {
            connector.LeaseUntil = null; connector.UpdatedAt = now;
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    public async Task<OperationalConnectorResponse?> GetAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var connector = await db.OperationalConnectors.AsNoTracking().SingleOrDefaultAsync(item => item.BusinessId == businessId, cancellationToken);
        return connector is null ? null : Response(connector);
    }

    public async Task<OperationalConnectorResponse?> UpdateScheduleAsync(Guid businessId, string schedule, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!OperationalSyncSchedules.IsSupported(schedule)) throw new ArgumentException("Unsupported sync schedule.", nameof(schedule));
        var connector = await db.OperationalConnectors.SingleOrDefaultAsync(item => item.BusinessId == businessId, cancellationToken);
        if (connector is null) return null;
        connector.Schedule = schedule; connector.UpdatedAt = now; await db.SaveChangesAsync(cancellationToken); return Response(connector);
    }

    public async Task<bool> DisconnectAsync(Guid businessId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connector = await db.OperationalConnectors.SingleOrDefaultAsync(item => item.BusinessId == businessId, cancellationToken);
        if (connector is null) return false;
        connector.Status = OperationalConnectorStates.Disconnected; connector.Schedule = OperationalSyncSchedules.Manual;
        connector.LeaseUntil = null; connector.UpdatedAt = now; await db.SaveChangesAsync(cancellationToken); return true;
    }

    private static OperationalConnectorResponse Response(OperationalConnector connector) => new(connector.Id, connector.FolderName,
        connector.Status, connector.Schedule, connector.LastAttemptAt, connector.LastSuccessAt, connector.ErrorCode);
}

public static class OperationalConnectorEndpoints
{
    public static void MapOperationalConnectorEndpoints(this WebApplication app)
    {
        const string path = "/api/v1/businesses/{businessId:guid}/operational-connector";
        app.MapGet(path, async (Guid businessId, ClaimsPrincipal user, AtlasDbContext db, OperationalConnectorService service, CancellationToken ct) =>
            !await IsOwner(businessId, user, db, ct) ? Results.NotFound() :
            await service.GetAsync(businessId, ct) is { } value ? Results.Ok(value) : Results.NotFound()).RequireAuthorization("BusinessOwner");
        app.MapPost(path, async (Guid businessId, ConnectOperationalConnectorRequest request, ClaimsPrincipal user, AtlasDbContext db, OperationalConnectorService service, CancellationToken ct) =>
            !await IsOwner(businessId, user, db, ct) ? Results.NotFound() : Results.Ok(await service.ConnectAsync(businessId, request.FolderId, request.Schedule, DateTimeOffset.UtcNow, ct))).RequireAuthorization("BusinessOwner");
        app.MapPut(path + "/schedule", async (Guid businessId, UpdateOperationalScheduleRequest request, ClaimsPrincipal user, AtlasDbContext db, OperationalConnectorService service, CancellationToken ct) =>
            !await IsOwner(businessId, user, db, ct) ? Results.NotFound() : await service.UpdateScheduleAsync(businessId, request.Schedule, DateTimeOffset.UtcNow, ct) is { } value ? Results.Ok(value) : Results.NotFound()).RequireAuthorization("BusinessOwner");
        app.MapPost(path + "/sync", async (Guid businessId, ClaimsPrincipal user, AtlasDbContext db, OperationalConnectorService service, CancellationToken ct) =>
            !await IsOwner(businessId, user, db, ct) ? Results.NotFound() : Results.Ok(await service.SyncBusinessAsync(businessId, DateTimeOffset.UtcNow, ct))).RequireAuthorization("BusinessOwner");
        app.MapDelete(path, async (Guid businessId, ClaimsPrincipal user, AtlasDbContext db, OperationalConnectorService service, CancellationToken ct) =>
            !await IsOwner(businessId, user, db, ct) ? Results.NotFound() : await service.DisconnectAsync(businessId, DateTimeOffset.UtcNow, ct) ? Results.NoContent() : Results.NotFound()).RequireAuthorization("BusinessOwner");
    }

    private static async Task<bool> IsOwner(Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return subject is not null && await db.BusinessMemberships.AnyAsync(item => item.BusinessId == businessId &&
            item.Role == MembershipRoles.BusinessOwner && item.UserAccount.ProviderSubject == subject, ct);
    }
}
