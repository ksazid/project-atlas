using System.Text.Json;

namespace Atlas.Api;

public sealed record OperationalChangeEvidence(
    Guid ChangeId,
    string MetricKey,
    int WindowDays,
    decimal CurrentValue,
    decimal ComparisonValue,
    decimal AbsoluteDelta,
    decimal RelativeDelta,
    string Freshness,
    string Confidence,
    IReadOnlyList<Guid> SignalIds);

public static class OperationalChangeEvidenceCodec
{
    private const string ObservedLanguage = "observed";

    public static ResolvedKnowledgeFact Encode(BusinessChange change, string freshness)
    {
        ArgumentNullException.ThrowIfNull(change);

        var windowDays = change.CurrentPeriodEnd.DayNumber - change.CurrentPeriodStart.DayNumber + 1;
        var signalIds = ParseSignalIds(change.EvidenceSignalIdsJson);
        var payload = new Payload(
            ObservedLanguage,
            change.Id,
            change.MetricKey,
            windowDays,
            change.CurrentValue,
            change.ComparisonValue,
            change.AbsoluteDelta,
            change.RelativeDelta,
            change.CurrentPeriodStart,
            change.CurrentPeriodEnd,
            change.ComparisonPeriodStart,
            change.ComparisonPeriodEnd,
            freshness,
            change.Confidence,
            signalIds);

        return new(
            KnowledgeEvidenceLayers.Operational,
            $"{change.MetricKey}-change-{windowDays}d",
            JsonSerializer.Serialize(payload),
            Source(change.Id, freshness, change.Confidence, signalIds));
    }

    public static bool TryParse(ResolvedKnowledgeFact fact, out OperationalChangeEvidence? evidence)
    {
        evidence = null;
        if (fact.Layer != KnowledgeEvidenceLayers.Operational ||
            !fact.Source.StartsWith("operational-change:", StringComparison.Ordinal) ||
            fact.Value.Contains("caused", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(fact.Value);
            if (payload is null || payload.Language != ObservedLanguage ||
                payload.ChangeId == Guid.Empty || !KnowledgePackKeys.IsValid(payload.MetricKey) ||
                payload.WindowDays is not (7 or 28) || payload.ComparisonValue == 0 ||
                payload.RelativeDelta is null ||
                payload.Freshness is not (OperationalFreshness.Fresh or OperationalFreshness.Stale) ||
                string.IsNullOrWhiteSpace(payload.Confidence) || payload.SignalIds.Count == 0 ||
                payload.SignalIds.Any(id => id == Guid.Empty) || payload.SignalIds.Distinct().Count() != payload.SignalIds.Count ||
                payload.CurrentPeriodEnd.DayNumber - payload.CurrentPeriodStart.DayNumber + 1 != payload.WindowDays ||
                fact.Key != $"{payload.MetricKey}-change-{payload.WindowDays}d")
                return false;

            var sortedSignalIds = payload.SignalIds.Order().ToArray();
            if (!string.Equals(
                    fact.Source,
                    Source(payload.ChangeId, payload.Freshness, payload.Confidence, sortedSignalIds),
                    StringComparison.Ordinal))
                return false;

            evidence = new(
                payload.ChangeId,
                payload.MetricKey,
                payload.WindowDays,
                payload.CurrentValue,
                payload.ComparisonValue,
                payload.AbsoluteDelta,
                payload.RelativeDelta.Value,
                payload.Freshness,
                payload.Confidence,
                sortedSignalIds);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyList<Guid> ParseSignalIds(string json)
    {
        try
        {
            return (JsonSerializer.Deserialize<Guid[]>(json) ?? [])
                .Distinct()
                .Order()
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string Source(Guid changeId, string freshness, string confidence, IEnumerable<Guid> signalIds) =>
        $"operational-change:{changeId:D}:{freshness}:{confidence}:signals:{string.Join(',', signalIds.Select(id => id.ToString("D")))}";

    private sealed record Payload(
        string Language,
        Guid ChangeId,
        string MetricKey,
        int WindowDays,
        decimal CurrentValue,
        decimal ComparisonValue,
        decimal AbsoluteDelta,
        decimal? RelativeDelta,
        DateOnly CurrentPeriodStart,
        DateOnly CurrentPeriodEnd,
        DateOnly ComparisonPeriodStart,
        DateOnly ComparisonPeriodEnd,
        string Freshness,
        string Confidence,
        IReadOnlyList<Guid> SignalIds);
}
