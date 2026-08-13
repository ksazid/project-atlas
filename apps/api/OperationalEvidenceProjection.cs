using System.Globalization;
using System.Text.Json;

namespace Atlas.Api;

public static class OperationalEvidenceProjector
{
    public static IReadOnlyList<ResolvedKnowledgeFact> Project(
        IReadOnlyCollection<BusinessSignal> signals,
        IReadOnlyCollection<BusinessChange> changes,
        DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(signals);
        ArgumentNullException.ThrowIfNull(changes);

        var facts = new List<ResolvedKnowledgeFact>();
        foreach (var signal in signals)
        {
            var freshness = OperationalIngestionService.ClassifyFreshness(signal.PeriodEnd, at);
            if (freshness == OperationalFreshness.Historical) continue;

            var measure = signal.Currency ?? signal.Unit;
            facts.Add(new(
                KnowledgeEvidenceLayers.Operational,
                signal.MetricKey,
                $"Observed {signal.MetricKey} {Format(signal.Value)} {measure} for {signal.PeriodStart:yyyy-MM-dd} to {signal.PeriodEnd:yyyy-MM-dd}.",
                $"operational-signal:{signal.Id:D}:{signal.SourceKind}:{freshness}:{signal.Confidence}"));
        }

        foreach (var change in changes)
        {
            var freshness = OperationalIngestionService.ClassifyFreshness(change.CurrentPeriodEnd, at);
            if (freshness == OperationalFreshness.Historical) continue;

            var window = change.CurrentPeriodEnd.DayNumber - change.CurrentPeriodStart.DayNumber + 1;
            var evidenceIds = EvidenceIds(change.EvidenceSignalIdsJson);
            facts.Add(new(
                KnowledgeEvidenceLayers.Operational,
                $"{change.MetricKey}-change-{window}d",
                $"Observed {change.MetricKey} change: current {Format(change.CurrentValue)}, comparison {Format(change.ComparisonValue)}, delta {Format(change.AbsoluteDelta)}.",
                $"operational-change:{change.Id:D}:{freshness}:{change.Confidence}:signals:{string.Join(',', evidenceIds)}"));
        }

        return facts.OrderBy(item => item.Key, StringComparer.Ordinal)
            .ThenBy(item => item.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Source, StringComparer.Ordinal)
            .ToArray();
    }

    private static string Format(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);

    private static IReadOnlyList<string> EvidenceIds(string json)
    {
        try
        {
            return (JsonSerializer.Deserialize<Guid[]>(json) ?? [])
                .Select(item => item.ToString("D"))
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
