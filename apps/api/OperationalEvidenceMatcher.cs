namespace Atlas.Api;

public static class OperationalEvidenceMatcher
{
    public static IReadOnlyList<ResolvedKnowledgeFact> Match(
        KnowledgeOperationalEvidenceRequirement requirement,
        IEnumerable<ResolvedKnowledgeFact> facts)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(facts);

        return facts
            .Where(fact => Matches(requirement, fact))
            .OrderBy(fact => fact.Key, StringComparer.Ordinal)
            .ThenBy(fact => fact.Value, StringComparer.Ordinal)
            .ThenBy(fact => fact.Source, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool Matches(
        KnowledgeOperationalEvidenceRequirement requirement,
        ResolvedKnowledgeFact fact)
    {
        if (!OperationalChangeEvidenceCodec.TryParse(fact, out var evidence) || evidence is null)
            return false;

        var direction = evidence.RelativeDelta < 0
            ? OperationalChangeDirections.Decrease
            : evidence.RelativeDelta > 0
                ? OperationalChangeDirections.Increase
                : null;

        return evidence.MetricKey == requirement.MetricKey &&
               direction == requirement.Direction &&
               Math.Abs(evidence.RelativeDelta) >= requirement.MinimumRelativeChange &&
               requirement.Windows.Contains(evidence.WindowDays) &&
               requirement.Freshness.Contains(evidence.Freshness, StringComparer.Ordinal);
    }
}
