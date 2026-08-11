using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Atlas.Api;

public sealed partial class MultiSourceBusinessDiscoveryService(
    BusinessDiscoveryService pageDiscovery,
    GoogleBusinessSourceResolver googleResolver,
    IBusinessLocationProvider locationProvider)
{
    public async Task<BusinessDiscoveryReconciliationResult> DiscoverAsync(
        string primaryUrl,
        IReadOnlyList<string>? additionalUrls,
        CancellationToken ct)
    {
        // Canonicalise the complete set before the first outbound request so an unsafe
        // secondary source can never be hidden behind a successful primary source.
        var sources = BusinessSourceUrlPolicy.CanonicalizeMany(primaryUrl, additionalUrls);
        var observations = new List<BusinessSourceObservation>(sources.Count);

        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            try
            {
                var snapshot = source.Kind == BusinessSourceKind.GoogleMaps
                    ? await DiscoverGoogleAsync(source, ct)
                    : await pageDiscovery.DiscoverAsync(source.Value, ct);

                observations.Add(new BusinessSourceObservation(
                    index,
                    index == 0,
                    snapshot.Provider,
                    source.Value,
                    "success",
                    snapshot.Facts));
            }
            catch (BusinessDiscoveryException ex) when (CanDegradeSource(ex.Code))
            {
                observations.Add(Failed(index, source, StatusFor(ex.Code), ex.Code));
            }
            catch (BusinessLocationProviderUnavailableException)
            {
                observations.Add(Failed(index, source, "unavailable", "business_location_provider_unavailable"));
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                observations.Add(Failed(index, source, "unavailable", "business_source_timeout"));
            }
            catch (HttpRequestException)
            {
                observations.Add(Failed(index, source, "unavailable", "business_source_unavailable"));
            }
        }

        return BusinessDiscoveryReconciler.Reconcile(observations);
    }

    private async Task<PublicBusinessSnapshot> DiscoverGoogleAsync(CanonicalBusinessUrl source, CancellationToken ct)
    {
        if (!locationProvider.IsConfigured)
            throw new BusinessLocationProviderUnavailableException("Business location search is not configured.");

        var resolved = await googleResolver.ResolveAsync(source, ct);
        var candidates = await locationProvider.SearchAsync(resolved.Query, ct);
        var selected = SelectSpecificCandidate(resolved.Query, candidates)
            ?? throw new BusinessDiscoveryException(
                "business_google_place_unresolved",
                "That Google Maps link could not be resolved to one business location. Share the specific business profile/location instead.");

        var observedAt = DateTimeOffset.UtcNow;
        var facts = new List<PublicBusinessFact>();
        Add("name", selected.Name, "high");
        Add("primaryLocation", selected.FormattedAddress, "high");
        Add("country", selected.CountryCode, "high");
        Add("timezone", selected.Timezone, "high");
        Add("currency", selected.Currency, "high");
        Add("description", selected.BusinessTypeSummary, "medium");

        return new PublicBusinessSnapshot("google-places", source.Value, observedAt, facts);

        void Add(string key, string? value, string confidence)
        {
            var cleaned = value?.Trim();
            if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length > PublicBusinessExtractor.MaxFactValueCharacters) return;
            facts.Add(new PublicBusinessFact(
                key,
                cleaned,
                "google-places",
                source.Value,
                observedAt,
                confidence));
        }
    }

    private static BusinessLocationCandidate? SelectSpecificCandidate(
        string resolvedQuery,
        IReadOnlyList<BusinessLocationCandidate> candidates)
    {
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        var expected = Normalize(resolvedQuery);
        var exact = candidates
            .Where(candidate => Normalize(candidate.Name).Equals(expected, StringComparison.Ordinal))
            .ToList();
        return exact.Count == 1 ? exact[0] : null;
    }

    private static BusinessSourceObservation Failed(
        int order,
        CanonicalBusinessUrl source,
        string status,
        string warningCode) =>
        new(
            order,
            order == 0,
            ProviderFor(source),
            source.Value,
            status,
            [],
            warningCode);

    private static string ProviderFor(CanonicalBusinessUrl source) => source.Kind switch
    {
        BusinessSourceKind.BoltFood => "bolt-food",
        BusinessSourceKind.Wolt => "wolt",
        BusinessSourceKind.GoogleMaps => "google-places",
        _ => "website",
    };

    private static string StatusFor(string code) =>
        code is "business_source_no_facts" or "business_google_place_unresolved" ? "no-facts" : "unavailable";

    private static bool CanDegradeSource(string code) => code is
        "business_source_unavailable" or
        "business_source_timeout" or
        "business_source_no_facts" or
        "business_source_invalid_content" or
        "business_source_redirected" or
        "business_google_source_unavailable" or
        "business_google_place_unresolved" or
        "business_google_redirect_limit" or
        "business_google_redirect_invalid";

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
