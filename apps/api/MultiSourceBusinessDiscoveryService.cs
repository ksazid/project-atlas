using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Atlas.Api;

internal static class OfficialWebsiteEnrichmentPolicy
{
    public static string? SelectWebsite(PublicBusinessSnapshot anchor)
    {
        var candidate = anchor.Facts
            .Where(fact => fact.Key.Equals("website", StringComparison.OrdinalIgnoreCase) &&
                           fact.Confidence.Equals("high", StringComparison.OrdinalIgnoreCase))
            .Select(fact => fact.Value.Trim())
            .FirstOrDefault(value => value.Length > 0);
        if (candidate is null || !PublicBusinessUrlPolicy.TryValidate(candidate, out var uri, out _) || uri is null)
            return null;
        return uri.AbsoluteUri;
    }

    public static bool StrongIdentityMatch(PublicBusinessSnapshot anchor, PublicBusinessSnapshot website)
    {
        var anchorName = Fact(anchor, "name");
        var websiteName = Fact(website, "name");
        if (string.IsNullOrWhiteSpace(anchorName) || string.IsNullOrWhiteSpace(websiteName) ||
            !Normalize(anchorName).Equals(Normalize(websiteName), StringComparison.Ordinal))
            return false;

        return SameSupportingFact(anchor, website, "phone") ||
               SameSupportingFact(anchor, website, "primaryLocation");
    }

    private static bool SameSupportingFact(PublicBusinessSnapshot left, PublicBusinessSnapshot right, string key)
    {
        var leftValue = Fact(left, key);
        var rightValue = Fact(right, key);
        return !string.IsNullOrWhiteSpace(leftValue) &&
               !string.IsNullOrWhiteSpace(rightValue) &&
               Normalize(leftValue).Equals(Normalize(rightValue), StringComparison.Ordinal);
    }

    private static string? Fact(PublicBusinessSnapshot snapshot, string key) =>
        snapshot.Facts.FirstOrDefault(fact => fact.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
        }
        return builder.ToString();
    }
}

public sealed partial class MultiSourceBusinessDiscoveryService(
    BusinessDiscoveryService pageDiscovery,
    GoogleBusinessSourceResolver googleResolver,
    IBusinessLocationProvider locationProvider)
{
    private static readonly HttpClient GoogleSourceClient = CreateGoogleSourceClient();

    public MultiSourceBusinessDiscoveryService(
        BusinessDiscoveryService pageDiscovery,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
        : this(
            pageDiscovery,
            new GoogleBusinessSourceResolver(GoogleSourceClient),
            new GoogleBusinessLocationProvider(httpClientFactory.CreateClient(), configuration))
    {
    }

    public async Task<BusinessDiscoveryReconciliationResult> DiscoverAsync(
        string primaryUrl,
        IReadOnlyList<string>? additionalUrls,
        CancellationToken ct)
    {
        // Canonicalise the complete set before the first outbound request so an unsafe
        // secondary source can never be hidden behind a successful primary source.
        var sources = BusinessSourceUrlPolicy.CanonicalizeMany(primaryUrl, additionalUrls);
        var observations = new List<BusinessSourceObservation>(sources.Count + 1);
        var successfulSnapshots = new Dictionary<int, PublicBusinessSnapshot>();

        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            try
            {
                var snapshot = source.Kind == BusinessSourceKind.GoogleMaps
                    ? await DiscoverGoogleAsync(source, ct)
                    : await pageDiscovery.DiscoverAsync(source.Value, ct);

                successfulSnapshots[index] = snapshot;
                observations.Add(new BusinessSourceObservation(
                    index,
                    index == 0,
                    snapshot.Provider,
                    source.Value,
                    "success",
                    snapshot.Facts,
                    Media: snapshot.Media,
                    Offerings: snapshot.Offerings));
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

        var primary = observations.FirstOrDefault(observation => observation.IsPrimary && observation.Status == "success");
        if (primary is not null && successfulSnapshots.TryGetValue(primary.Order, out var anchorSnapshot))
        {
            var websiteUrl = OfficialWebsiteEnrichmentPolicy.SelectWebsite(anchorSnapshot);
            if (websiteUrl is not null && !AlreadySupplied(websiteUrl, sources))
            {
                try
                {
                    var websiteSnapshot = await pageDiscovery.DiscoverAsync(websiteUrl, ct);
                    if (OfficialWebsiteEnrichmentPolicy.StrongIdentityMatch(anchorSnapshot, websiteSnapshot))
                    {
                        var order = observations.Count;
                        observations.Add(new BusinessSourceObservation(
                            order,
                            false,
                            websiteSnapshot.Provider,
                            websiteSnapshot.SourceUrl,
                            "success",
                            websiteSnapshot.Facts,
                            Media: websiteSnapshot.Media,
                            Offerings: websiteSnapshot.Offerings));
                    }
                }
                catch (BusinessDiscoveryException ex) when (CanDegradeSource(ex.Code))
                {
                    // Optional enrichment cannot invalidate a usable owner-supplied source.
                }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested)
                {
                    // Optional enrichment timeout degrades to the accepted anchor.
                }
                catch (HttpRequestException)
                {
                    // Optional enrichment transport failure degrades to the accepted anchor.
                }
            }
        }

        return BusinessDiscoveryReconciler.Reconcile(observations);
    }

    private static bool AlreadySupplied(string websiteUrl, IReadOnlyList<CanonicalBusinessUrl> sources)
    {
        if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var website)) return true;
        return sources.Any(source =>
            Uri.TryCreate(source.Value, UriKind.Absolute, out var supplied) &&
            string.Equals(website.Scheme, supplied.Scheme, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(website.IdnHost.TrimEnd('.'), supplied.IdnHost.TrimEnd('.'), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(website.AbsolutePath.TrimEnd('/'), supplied.AbsolutePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
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

    private static HttpClient CreateGoogleSourceClient()
    {
        var client = new HttpClient(GoogleBusinessSourceHttpHandlerFactory.Create(), disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(8),
        };
        return client;
    }

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
