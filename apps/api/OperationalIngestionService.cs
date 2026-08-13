using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Atlas.Api;

public static class OperationalIngestionStates
{
    public const string Imported = "imported";
    public const string Duplicate = "duplicate";
    public const string OverlapConflict = "overlap-conflict";
}

public static class OperationalFreshness
{
    public const string Fresh = "fresh";
    public const string Stale = "stale";
    public const string Historical = "historical";
}

public sealed record OperationalIngestionResult(string State, int CreatedSignals, int CreatedChanges, string Freshness);

public sealed class OperationalIngestionService(AtlasDbContext db)
{
    public async Task<OperationalIngestionResult> IngestAsync(
        Guid businessId,
        string sourceKind,
        string sourceReference,
        string importFingerprint,
        OperationalNormalizationResult normalized,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        var freshness = ClassifyFreshness(normalized.Preview.LatestBusinessDate, observedAt);
        if (await db.OperationalImports.AnyAsync(item =>
                item.BusinessId == businessId && item.ImportFingerprint == importFingerprint, cancellationToken))
            return new(OperationalIngestionStates.Duplicate, 0, 0, freshness);

        var candidates = normalized.Observations.Select(observation => new SignalCandidate(
            SignalIdentity(businessId, sourceKind, observation), observation)).ToArray();
        var identities = candidates.Select(candidate => candidate.Identity).ToArray();
        var existing = await db.BusinessSignals
            .Where(item => item.BusinessId == businessId && identities.Contains(item.Identity))
            .ToDictionaryAsync(item => item.Identity, StringComparer.Ordinal, cancellationToken);

        foreach (var candidate in candidates)
        {
            if (!existing.TryGetValue(candidate.Identity, out var signal)) continue;
            if (!Equivalent(signal, candidate.Observation))
                return new(OperationalIngestionStates.OverlapConflict, 0, 0, freshness);
        }

        var pending = candidates.Where(candidate => !existing.ContainsKey(candidate.Identity)).ToArray();
        if (pending.Length == 0)
            return new(OperationalIngestionStates.Duplicate, 0, 0, freshness);

        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational())
            transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = observedAt.ToUniversalTime();
            var import = new OperationalImport
            {
                Id = Guid.NewGuid(), BusinessId = businessId, SourceKind = sourceKind,
                ImportFingerprint = importFingerprint, Status = OperationalIngestionStates.Imported,
                AcceptedRows = normalized.Preview.RowCount,
                IgnoredColumns = normalized.Preview.IgnoredSensitiveColumns.Count,
                EarliestBusinessDate = normalized.Preview.EarliestBusinessDate,
                LatestBusinessDate = normalized.Preview.LatestBusinessDate,
                CreatedAt = now, CompletedAt = now
            };
            db.OperationalImports.Add(import);

            var confidence = freshness switch
            {
                OperationalFreshness.Fresh => "high",
                OperationalFreshness.Stale => "medium",
                _ => "low"
            };
            foreach (var candidate in pending)
            {
                var observation = candidate.Observation;
                db.BusinessSignals.Add(new BusinessSignal
                {
                    Id = Guid.NewGuid(), BusinessId = businessId, OperationalImportId = import.Id,
                    Identity = candidate.Identity, MetricKey = observation.MetricKey, Value = observation.Value,
                    Unit = observation.Unit, Currency = observation.Currency,
                    PeriodStart = observation.PeriodStart, PeriodEnd = observation.PeriodEnd,
                    DimensionsJson = CanonicalDimensions(observation.Dimensions), SourceKind = sourceKind,
                    SourceReference = sourceReference, ObservedAt = now, Confidence = confidence
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            var createdChanges = await DeriveChangesAsync(businessId, observedAt, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(OperationalIngestionStates.Imported, pending.Length, createdChanges, freshness);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public static string ClassifyFreshness(DateOnly latestBusinessDate, DateTimeOffset at)
    {
        var age = DateOnly.FromDateTime(at.UtcDateTime).DayNumber - latestBusinessDate.DayNumber;
        if (age <= 7) return OperationalFreshness.Fresh;
        if (age <= 30) return OperationalFreshness.Stale;
        return OperationalFreshness.Historical;
    }

    private async Task<int> DeriveChangesAsync(Guid businessId, DateTimeOffset observedAt, CancellationToken cancellationToken)
    {
        var signals = await db.BusinessSignals.Where(item => item.BusinessId == businessId).ToListAsync(cancellationToken);
        var created = 0;
        foreach (var series in signals.GroupBy(item => new
                 {
                     item.MetricKey, item.Unit, item.Currency, Dimensions = item.DimensionsJson ?? ""
                 }))
        {
            var daily = series.Where(item => item.PeriodStart == item.PeriodEnd)
                .GroupBy(item => item.PeriodStart)
                .ToDictionary(group => group.Key, group => group.ToArray());
            if (daily.Count == 0) continue;
            var latest = daily.Keys.Max();
            foreach (var window in new[] { 7, 28 })
            {
                var currentStart = latest.AddDays(-(window - 1));
                var comparisonEnd = currentStart.AddDays(-1);
                var comparisonStart = comparisonEnd.AddDays(-(window - 1));
                if (!Complete(daily, currentStart, latest) || !Complete(daily, comparisonStart, comparisonEnd)) continue;

                var currentSignals = Range(daily, currentStart, latest).ToArray();
                var comparisonSignals = Range(daily, comparisonStart, comparisonEnd).ToArray();
                var current = currentSignals.Sum(item => item.Value);
                var comparison = comparisonSignals.Sum(item => item.Value);
                var identity = ChangeIdentity(businessId, series.Key.MetricKey, series.Key.Unit, series.Key.Currency,
                    series.Key.Dimensions, window, latest);
                var change = await db.BusinessChanges.SingleOrDefaultAsync(item =>
                    item.BusinessId == businessId && item.Identity == identity, cancellationToken);
                if (change is null)
                {
                    change = new BusinessChange
                    {
                        Id = Guid.NewGuid(), BusinessId = businessId, Identity = identity,
                        MetricKey = series.Key.MetricKey, EvidenceSignalIdsJson = "[]", Confidence = "low"
                    };
                    db.BusinessChanges.Add(change);
                    created++;
                }

                change.CurrentValue = current;
                change.ComparisonValue = comparison;
                change.AbsoluteDelta = current - comparison;
                change.RelativeDelta = comparison == 0m ? null : (current - comparison) / Math.Abs(comparison);
                change.CurrentPeriodStart = currentStart;
                change.CurrentPeriodEnd = latest;
                change.ComparisonPeriodStart = comparisonStart;
                change.ComparisonPeriodEnd = comparisonEnd;
                change.EvidenceSignalIdsJson = JsonSerializer.Serialize(currentSignals.Concat(comparisonSignals).Select(item => item.Id));
                change.ObservedAt = observedAt.ToUniversalTime();
                change.Confidence = series.Any(item => item.Confidence == "low") ? "low" :
                    series.Any(item => item.Confidence == "medium") ? "medium" : "high";
            }
        }
        return created;
    }

    private static bool Complete(IReadOnlyDictionary<DateOnly, BusinessSignal[]> daily, DateOnly start, DateOnly end)
    {
        for (var date = start; date <= end; date = date.AddDays(1))
            if (!daily.ContainsKey(date)) return false;
        return true;
    }

    private static IEnumerable<BusinessSignal> Range(IReadOnlyDictionary<DateOnly, BusinessSignal[]> daily, DateOnly start, DateOnly end)
    {
        for (var date = start; date <= end; date = date.AddDays(1))
            foreach (var signal in daily[date]) yield return signal;
    }

    private static bool Equivalent(BusinessSignal signal, OperationalObservation observation) =>
        signal.MetricKey == observation.MetricKey && signal.Value == observation.Value &&
        signal.Unit == observation.Unit && signal.Currency == observation.Currency &&
        signal.PeriodStart == observation.PeriodStart && signal.PeriodEnd == observation.PeriodEnd &&
        (signal.DimensionsJson ?? "") == (CanonicalDimensions(observation.Dimensions) ?? "");

    private static string SignalIdentity(Guid businessId, string sourceKind, OperationalObservation observation) => Hash(
        string.Join('|', businessId.ToString("N"), sourceKind.Trim().ToLowerInvariant(), observation.MetricKey,
            observation.Unit, observation.Currency ?? "", observation.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            observation.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), CanonicalDimensions(observation.Dimensions) ?? ""));

    private static string ChangeIdentity(Guid businessId, string metric, string unit, string? currency, string dimensions, int window, DateOnly latest) =>
        Hash(string.Join('|', businessId.ToString("N"), metric, unit, currency ?? "", dimensions, window, latest.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

    private static string? CanonicalDimensions(IReadOnlyDictionary<string, string>? dimensions)
    {
        if (dimensions is null || dimensions.Count == 0) return null;
        return JsonSerializer.Serialize(dimensions.OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal));
    }

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private sealed record SignalCandidate(string Identity, OperationalObservation Observation);
}
