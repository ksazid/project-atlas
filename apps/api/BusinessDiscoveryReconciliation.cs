using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Atlas.Api;

public sealed record BusinessSourceObservation(
    int Order,
    bool IsPrimary,
    string Provider,
    string CanonicalUrl,
    string Status,
    IReadOnlyList<PublicBusinessFact> Facts,
    string? WarningCode = null,
    IReadOnlyList<PublicBusinessMedia>? Media = null,
    IReadOnlyList<PublicBusinessOffering>? Offerings = null);

public sealed record BusinessSourceResult(
    int Order,
    bool IsPrimary,
    string Provider,
    string CanonicalUrl,
    DateTimeOffset ObservedAt,
    string Status,
    string AssociationStatus,
    string? WarningCode);

public sealed record BusinessDiscoveryEvidenceCandidate(
    int SourceOrder,
    string Provider,
    string CanonicalUrl,
    string Key,
    string Value,
    DateTimeOffset ObservedAt,
    string Confidence,
    string EvidenceClass,
    string ReconciliationState,
    string AssociationStatus);

public sealed record BusinessDiscoveryReconciliationResult(
    PublicBusinessSnapshot Snapshot,
    IReadOnlyList<BusinessSourceResult> SourceResults,
    IReadOnlyList<BusinessDiscoveryEvidenceCandidate> Evidence,
    IReadOnlyList<string> Warnings);

public static partial class BusinessDiscoveryReconciler
{
    private const string Success = "success";

    public static BusinessDiscoveryReconciliationResult Reconcile(IReadOnlyList<BusinessSourceObservation> observations)
    {
        if (observations.Count == 0)
            throw new BusinessDiscoveryException("business_sources_no_facts", "Atlas could not find useful business details from the supplied public pages.");

        var sources = observations.OrderBy(x => x.Order).ToList();
        var anchor = sources.FirstOrDefault(IsUsableSource)
            ?? throw new BusinessDiscoveryException("business_sources_no_facts", "Atlas could not find useful business details from the supplied public pages.");

        var warnings = new HashSet<string>(StringComparer.Ordinal);
        var sourceResults = new List<BusinessSourceResult>(sources.Count);
        var evidence = new List<BusinessDiscoveryEvidenceCandidate>();
        var acceptedSources = new List<(BusinessSourceObservation Source, string Association)>();

        foreach (var source in sources)
        {
            if (!string.IsNullOrWhiteSpace(source.WarningCode)) warnings.Add(source.WarningCode!);

            if (!IsUsableSource(source))
            {
                sourceResults.Add(Result(source, "unavailable"));
                continue;
            }

            if (ReferenceEquals(source, anchor))
            {
                acceptedSources.Add((source, "anchor"));
                sourceResults.Add(Result(source, "anchor"));
                continue;
            }

            var association = Associate(anchor, source);
            sourceResults.Add(Result(source, association));

            if (association == "strong")
            {
                acceptedSources.Add((source, association));
                continue;
            }

            warnings.Add(association == "mismatch"
                ? "business_source_identity_mismatch"
                : "business_source_identity_ambiguous");
            foreach (var fact in source.Facts.Where(IsUsableFact))
                evidence.Add(Evidence(source, fact, "excluded", association));
        }

        var selected = new Dictionary<string, (PublicBusinessFact Fact, BusinessSourceObservation Source)>(StringComparer.OrdinalIgnoreCase);
        var selectedMedia = new Dictionary<string, PublicBusinessMedia>(StringComparer.OrdinalIgnoreCase);
        var selectedOfferings = new Dictionary<string, PublicBusinessOffering>(StringComparer.OrdinalIgnoreCase);

        foreach (var (source, association) in acceptedSources.OrderBy(x => x.Source.Order))
        {
            foreach (var fact in source.Facts.Where(IsUsableFact))
            {
                var canonicalFact = fact with
                {
                    Source = source.Provider,
                    SourceUrl = source.CanonicalUrl,
                };

                if (!selected.TryGetValue(fact.Key, out var existing))
                {
                    selected[fact.Key] = (canonicalFact, source);
                    evidence.Add(Evidence(source, canonicalFact, "selected", association));
                    continue;
                }

                if (Equivalent(existing.Fact.Value, canonicalFact.Value))
                {
                    evidence.Add(Evidence(source, canonicalFact, "corroborating", association));
                    continue;
                }

                evidence.Add(Evidence(source, canonicalFact, "conflict", association));
                warnings.Add("business_source_conflict");
            }

            foreach (var media in (source.Media ?? []).Take(PublicBusinessMediaMenuExtractor.MaxMediaPerSource))
            {
                if (string.IsNullOrWhiteSpace(media.RemoteUrl)) continue;
                var canonical = media with
                {
                    Source = source.Provider,
                    SourceUrl = source.CanonicalUrl,
                    SourceOrder = source.Order,
                    OwnerConfirmed = false
                };
                selectedMedia.TryAdd(canonical.RemoteUrl.Trim(), canonical);
            }

            foreach (var offering in (source.Offerings ?? []).Take(PublicBusinessMediaMenuExtractor.MaxOfferingsPerSource))
            {
                if (string.IsNullOrWhiteSpace(offering.Kind) || string.IsNullOrWhiteSpace(offering.Name)) continue;
                var canonical = offering with
                {
                    Source = source.Provider,
                    SourceUrl = source.CanonicalUrl,
                    SourceOrder = source.Order,
                    OwnerConfirmed = false
                };
                selectedOfferings.TryAdd(OfferingKey(canonical), canonical);
            }
        }

        if (selected.Count == 0)
            throw new BusinessDiscoveryException("business_sources_no_facts", "Atlas could not find useful business details from the supplied public pages.");

        var observedAt = SourceObservedAt(anchor);
        var snapshot = new PublicBusinessSnapshot(
            anchor.Provider,
            anchor.CanonicalUrl,
            observedAt,
            selected.Values.Select(x => x.Fact).ToList())
        {
            Media = selectedMedia.Values.ToList(),
            Offerings = selectedOfferings.Values.ToList()
        };

        return new BusinessDiscoveryReconciliationResult(
            snapshot,
            sourceResults,
            evidence,
            warnings.Order(StringComparer.Ordinal).ToList());
    }

    private static BusinessSourceResult Result(BusinessSourceObservation source, string association) =>
        new(source.Order, source.IsPrimary, source.Provider, source.CanonicalUrl, SourceObservedAt(source), source.Status, association, source.WarningCode);

    private static BusinessDiscoveryEvidenceCandidate Evidence(
        BusinessSourceObservation source,
        PublicBusinessFact fact,
        string state,
        string association) =>
        new(
            source.Order,
            source.Provider,
            source.CanonicalUrl,
            fact.Key,
            fact.Value,
            fact.ObservedAt,
            fact.Confidence,
            fact.EvidenceClass,
            state,
            association);

    private static DateTimeOffset SourceObservedAt(BusinessSourceObservation source) =>
        source.Facts.Where(IsUsableFact).Select(x => x.ObservedAt)
            .Concat((source.Media ?? []).Select(x => x.ObservedAt))
            .Concat((source.Offerings ?? []).Select(x => x.ObservedAt))
            .DefaultIfEmpty(DateTimeOffset.UtcNow)
            .Min();

    private static bool IsUsableSource(BusinessSourceObservation source) =>
        source.Status.Equals(Success, StringComparison.OrdinalIgnoreCase) && source.Facts.Any(IsUsableFact);

    private static bool IsUsableFact(PublicBusinessFact fact) =>
        !string.IsNullOrWhiteSpace(fact.Key) && !string.IsNullOrWhiteSpace(fact.Value);

    private static string Associate(BusinessSourceObservation anchor, BusinessSourceObservation candidate)
    {
        var anchorName = Fact(anchor, "name");
        var candidateName = Fact(candidate, "name");
        var anchorLocation = Fact(anchor, "primaryLocation");
        var candidateLocation = Fact(candidate, "primaryLocation");

        var nameMatch = Related(anchorName, candidateName);
        var locationMatch = Related(anchorLocation, candidateLocation);

        if (nameMatch || locationMatch) return "strong";

        if (!string.IsNullOrWhiteSpace(anchorName) && !string.IsNullOrWhiteSpace(candidateName))
            return "mismatch";
        if (!string.IsNullOrWhiteSpace(anchorLocation) && !string.IsNullOrWhiteSpace(candidateLocation))
            return "mismatch";

        return "ambiguous";
    }

    private static string? Fact(BusinessSourceObservation source, string key) =>
        source.Facts.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;

    private static bool Related(string? left, string? right)
    {
        var a = Normalize(left);
        var b = Normalize(right);
        if (a.Length == 0 || b.Length == 0) return false;
        if (a.Equals(b, StringComparison.Ordinal)) return true;

        var compactA = a.Replace(" ", string.Empty, StringComparison.Ordinal);
        var compactB = b.Replace(" ", string.Empty, StringComparison.Ordinal);
        var shorter = Math.Min(compactA.Length, compactB.Length);
        return shorter >= 6 &&
            (compactA.Contains(compactB, StringComparison.Ordinal) || compactB.Contains(compactA, StringComparison.Ordinal));
    }

    private static bool Equivalent(string left, string right) =>
        Normalize(left).Equals(Normalize(right), StringComparison.Ordinal);

    private static string OfferingKey(PublicBusinessOffering offering) => string.Join("|",
        offering.Kind.Trim().ToLowerInvariant(),
        offering.Section?.Trim().ToLowerInvariant() ?? string.Empty,
        offering.Name.Trim().ToLowerInvariant(),
        offering.Price?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        offering.Currency?.Trim().ToUpperInvariant() ?? string.Empty);

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ');
        }
        return WhitespaceRegex().Replace(builder.ToString(), " ").Trim();
    }

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
