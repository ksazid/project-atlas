using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalIngestionServiceTests
{
    private static readonly Guid BusinessId = Guid.Parse("10000000-0000-0000-0000-000000000038");
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Same_import_fingerprint_is_idempotent()
    {
        await using var db = CreateDb();
        var service = new OperationalIngestionService(db);
        var normalized = Result(Observation(new DateOnly(2026, 8, 13), 100m));

        var first = await service.IngestAsync(BusinessId, "google-drive", "file-1", "bytes-a", normalized, Now, default);
        var second = await service.IngestAsync(BusinessId, "google-drive", "file-1", "bytes-a", normalized, Now, default);

        Assert.Equal(OperationalIngestionStates.Imported, first.State);
        Assert.Equal(OperationalIngestionStates.Duplicate, second.State);
        Assert.Single(db.OperationalImports);
        Assert.Single(db.BusinessSignals);
    }

    [Fact]
    public async Task Byte_different_import_with_same_normalized_observation_does_not_duplicate_signal()
    {
        await using var db = CreateDb();
        var service = new OperationalIngestionService(db);
        var normalized = Result(Observation(new DateOnly(2026, 8, 13), 100m));

        await service.IngestAsync(BusinessId, "google-drive", "file-1", "bytes-a", normalized, Now, default);
        var replay = await service.IngestAsync(BusinessId, "google-drive", "file-2", "bytes-b", normalized, Now, default);

        Assert.Equal(OperationalIngestionStates.Duplicate, replay.State);
        Assert.Single(db.BusinessSignals);
    }

    [Fact]
    public async Task Conflicting_overlap_is_rejected_without_partial_persistence()
    {
        await using var db = CreateDb();
        var service = new OperationalIngestionService(db);
        var date = new DateOnly(2026, 8, 13);
        await service.IngestAsync(BusinessId, "google-drive", "file-1", "bytes-a", Result(Observation(date, 100m)), Now, default);

        var conflict = await service.IngestAsync(BusinessId, "google-drive", "file-2", "bytes-b", Result(Observation(date, 120m)), Now, default);

        Assert.Equal(OperationalIngestionStates.OverlapConflict, conflict.State);
        Assert.Single(db.OperationalImports);
        Assert.Equal(100m, Assert.Single(db.BusinessSignals).Value);
    }

    [Theory]
    [InlineData(14, 7, 140, 70, 1.0)]
    [InlineData(56, 28, 560, 280, 1.0)]
    public async Task Complete_matching_windows_derive_changes(int days, int window, decimal current, decimal comparison, double relative)
    {
        await using var db = CreateDb();
        var service = new OperationalIngestionService(db);
        var start = new DateOnly(2026, 6, 19);
        var observations = Enumerable.Range(0, days).Select(index =>
            Observation(start.AddDays(index), index < days - window ? 10m : 20m)).ToArray();

        await service.IngestAsync(BusinessId, "google-drive", "file-window", $"bytes-{window}", Result(observations), Now, default);

        var change = Assert.Single(db.BusinessChanges.Where(item =>
            item.CurrentPeriodEnd.DayNumber - item.CurrentPeriodStart.DayNumber + 1 == window));
        Assert.Equal(current, change.CurrentValue);
        Assert.Equal(comparison, change.ComparisonValue);
        Assert.Equal(current - comparison, change.AbsoluteDelta);
        Assert.Equal((decimal)relative, change.RelativeDelta);
        Assert.NotEqual("[]", change.EvidenceSignalIdsJson);
    }

    [Fact]
    public async Task Incomplete_windows_do_not_fabricate_changes_and_zero_comparison_has_no_relative_delta()
    {
        await using var db = CreateDb();
        var service = new OperationalIngestionService(db);
        var start = new DateOnly(2026, 7, 31);
        var incomplete = Enumerable.Range(0, 13).Select(index => Observation(start.AddDays(index), 10m)).ToArray();
        await service.IngestAsync(BusinessId, "google-drive", "file-short", "bytes-short", Result(incomplete), Now, default);
        Assert.Empty(db.BusinessChanges);

        var complete = Enumerable.Range(0, 14).Select(index => Observation(start.AddDays(index), index < 7 ? 0m : 10m)).ToArray();
        await using var secondDb = CreateDb();
        await new OperationalIngestionService(secondDb).IngestAsync(BusinessId, "google-drive", "file-zero", "bytes-zero", Result(complete), Now, default);
        Assert.Null(Assert.Single(secondDb.BusinessChanges).RelativeDelta);
    }

    [Theory]
    [InlineData(7, OperationalFreshness.Fresh)]
    [InlineData(8, OperationalFreshness.Stale)]
    [InlineData(30, OperationalFreshness.Stale)]
    [InlineData(31, OperationalFreshness.Historical)]
    public void Freshness_uses_latest_business_date_not_import_time(int ageDays, string expected)
    {
        Assert.Equal(expected, OperationalIngestionService.ClassifyFreshness(DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-ageDays), Now));
    }

    private static OperationalObservation Observation(DateOnly date, decimal value) =>
        new("gross-sales", value, "currency", "EUR", date, date, null);

    private static OperationalNormalizationResult Result(params OperationalObservation[] observations)
    {
        var dates = observations.Select(item => item.PeriodStart).ToArray();
        return new(new OperationalCsvPreview(observations.Length, observations.Length, dates.Min(), dates.Max(),
            ["date", "gross sales"], [], ["gross-sales"], "preview-fingerprint"), observations);
    }

    private static AtlasDbContext CreateDb() => new(new DbContextOptionsBuilder<AtlasDbContext>()
        .UseInMemoryDatabase($"operational-ingestion-{Guid.NewGuid():N}").Options);
}
