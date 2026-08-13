using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalConnectorServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Connect_validates_folder_and_persists_one_read_only_connector()
    {
        await using var db = CreateDb();
        var business = SeedBusiness(db);
        var source = new FakeSource { Folder = new("folder-38", "Atlas exports") };
        var service = new OperationalConnectorService(db, source);

        var result = await service.ConnectAsync(business.Id, "folder-38", OperationalSyncSchedules.Daily, Now, default);

        Assert.Equal(OperationalConnectorStates.Connected, result.Status);
        var connector = Assert.Single(db.OperationalConnectors);
        Assert.Equal("folder-38", connector.FolderId);
        Assert.Equal("google-drive", connector.SourceKind);
        Assert.Equal(1, source.ValidateCalls);
    }

    [Fact]
    public async Task Active_lease_prevents_competing_sync()
    {
        await using var db = CreateDb();
        var business = SeedBusiness(db);
        db.OperationalConnectors.Add(Connector(business.Id, Now.AddMinutes(5)));
        await db.SaveChangesAsync();

        var result = await new OperationalConnectorService(db, new FakeSource()).SyncBusinessAsync(business.Id, Now, default);

        Assert.Equal(OperationalSyncStates.Busy, result.State);
    }

    [Fact]
    public async Task Sync_skips_unchanged_file_id_metadata_and_checksum()
    {
        await using var db = CreateDb();
        var business = SeedBusiness(db);
        var connector = Connector(business.Id, null);
        db.OperationalConnectors.Add(connector);
        db.OperationalFileCheckpoints.Add(new OperationalFileCheckpoint
        {
            Id = Guid.NewGuid(), BusinessId = business.Id, ConnectorId = connector.Id, ProviderFileId = "csv-1",
            FileName = "sales.csv", MimeType = "text/csv", Size = 100, ProviderModifiedAt = Now.AddHours(-1),
            ContentFingerprint = "checksum-a", ProcessedAt = Now.AddHours(-1)
        });
        await db.SaveChangesAsync();
        var source = new FakeSource
        {
            Files = [new("csv-1", "sales.csv", "text/csv", Now.AddHours(-1), 100, "checksum-a")]
        };

        var result = await new OperationalConnectorService(db, source).SyncBusinessAsync(business.Id, Now, default);

        Assert.Equal(OperationalSyncStates.Completed, result.State);
        Assert.Equal(1, result.UnchangedFiles);
        Assert.Equal(0, source.OpenCalls);
    }

    [Fact]
    public async Task Revoked_grant_marks_reauthorization_required()
    {
        await using var db = CreateDb();
        var business = SeedBusiness(db);
        db.OperationalConnectors.Add(Connector(business.Id, null));
        await db.SaveChangesAsync();
        var source = new FakeSource { ListError = new(GoogleDriveOperationalErrorCodes.ReauthorizationRequired, "safe") };

        var result = await new OperationalConnectorService(db, source).SyncBusinessAsync(business.Id, Now, default);

        Assert.Equal(OperationalSyncStates.ReauthorizationRequired, result.State);
        Assert.Equal(OperationalConnectorStates.ReauthorizationRequired, db.OperationalConnectors.Single().Status);
    }

    private static OperationalConnector Connector(Guid businessId, DateTimeOffset? lease) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, SourceKind = "google-drive", FolderId = "folder-38",
        FolderName = "Atlas exports", Status = OperationalConnectorStates.Connected, Schedule = OperationalSyncSchedules.Daily,
        LeaseUntil = lease, CreatedAt = Now.AddDays(-1), UpdatedAt = Now.AddDays(-1)
    };

    private static Business SeedBusiness(AtlasDbContext db)
    {
        var business = Business.Create(new("Atlas Cafe", "restaurant-cafe", "MT", "Europe/Malta", "EUR", "Valletta", "open"));
        db.Businesses.Add(business);
        db.SaveChanges();
        return business;
    }

    private static AtlasDbContext CreateDb() => new(new DbContextOptionsBuilder<AtlasDbContext>()
        .UseInMemoryDatabase($"connector-{Guid.NewGuid():N}").Options);

    private sealed class FakeSource : IOperationalFileSource
    {
        public OperationalDriveFolder Folder { get; set; } = new("folder-38", "Atlas exports");
        public IReadOnlyList<OperationalSourceFile> Files { get; set; } = [];
        public GoogleDriveOperationalException? ListError { get; set; }
        public int ValidateCalls { get; private set; }
        public int OpenCalls { get; private set; }
        public Task<OperationalDriveFolder> ValidateFolderAsync(string folderId, CancellationToken cancellationToken) { ValidateCalls++; return Task.FromResult(Folder); }
        public Task<IReadOnlyList<OperationalSourceFile>> ListAsync(string folderId, CancellationToken cancellationToken) =>
            ListError is null ? Task.FromResult(Files) : Task.FromException<IReadOnlyList<OperationalSourceFile>>(ListError);
        public Task<Stream> OpenReadAsync(string fileId, CancellationToken cancellationToken) { OpenCalls++; return Task.FromResult<Stream>(new MemoryStream()); }
    }
}
