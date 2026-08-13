using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Atlas.Api;

public static class KnowledgeEvidenceLayers
{
    public const string Context = "context";
    public const string LocalMarket = "local-market";
    public const string Memory = "memory";
    public const string Operational = "operational";
}

public sealed record ResolvedKnowledgeManifest(string Layer, string PackKey, string ExactVersion, string Fingerprint);
public sealed record ResolvedKnowledgeFact(string Layer, string Key, string Value, string Source);
public sealed record ResolvedKnowledgeBundle(
    string CategoryKey,
    string? SubcategoryKey,
    IReadOnlyList<ResolvedKnowledgeManifest> Manifests,
    IReadOnlyList<ResolvedKnowledgeFact> ContextFacts,
    IReadOnlyList<ResolvedKnowledgeFact> LocalMarketFacts,
    IReadOnlyList<ResolvedKnowledgeFact> MemoryFacts,
    string Fingerprint)
{
    public IReadOnlyList<ResolvedKnowledgeFact> OperationalFacts { get; init; } = [];
}

public sealed class KnowledgeBundleResolutionException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public static class KnowledgeBundleResolver
{
    public static ResolvedKnowledgeBundle Resolve(
        Business business,
        BusinessKnowledgeAssignment coreAssignment,
        IReadOnlyCollection<BusinessProfileField> profileFields,
        IReadOnlyCollection<BusinessContextEntry> contextEntries,
        IReadOnlyCollection<BusinessMemoryItem> memoryItems) =>
        Resolve(business, coreAssignment, profileFields, contextEntries, memoryItems, []);

    public static ResolvedKnowledgeBundle Resolve(
        Business business,
        BusinessKnowledgeAssignment coreAssignment,
        IReadOnlyCollection<BusinessProfileField> profileFields,
        IReadOnlyCollection<BusinessContextEntry> contextEntries,
        IReadOnlyCollection<BusinessMemoryItem> memoryItems,
        IReadOnlyCollection<ResolvedKnowledgeFact> operationalFacts)
    {
        ArgumentNullException.ThrowIfNull(business);
        ArgumentNullException.ThrowIfNull(coreAssignment);
        ArgumentNullException.ThrowIfNull(profileFields);
        ArgumentNullException.ThrowIfNull(contextEntries);
        ArgumentNullException.ThrowIfNull(memoryItems);
        ArgumentNullException.ThrowIfNull(operationalFacts);

        var core = GenericBusinessKnowledgeManifestV2.Create();
        EnsureCoreAssignment(business, coreAssignment, core);

        var categoryKey = business.Category.Trim().ToLowerInvariant();
        var subcategoryKey = ResolveSubcategory(business.Id, categoryKey, profileFields);
        var manifests = ResolveManifests(categoryKey, core);
        var contextFacts = ResolveContext(business.Id, contextEntries);
        var localMarketFacts = ResolveLocalMarket(business);
        var memoryFacts = ResolveMemory(business.Id, memoryItems);
        var resolvedOperationalFacts = CanonicalOperationalFacts(operationalFacts);
        var fingerprint = Fingerprint(categoryKey, subcategoryKey, manifests, contextFacts, localMarketFacts, memoryFacts, resolvedOperationalFacts);

        return new ResolvedKnowledgeBundle(
            categoryKey,
            subcategoryKey,
            manifests,
            contextFacts,
            localMarketFacts,
            memoryFacts,
            fingerprint)
        {
            OperationalFacts = resolvedOperationalFacts
        };
    }

    private static void EnsureCoreAssignment(Business business, BusinessKnowledgeAssignment assignment, KnowledgePackManifestV2 core)
    {
        if (assignment.BusinessId != business.Id)
            throw new KnowledgeBundleResolutionException("core_assignment_business_mismatch", "The current Core Knowledge Pack assignment does not belong to this Business.");
        if (!assignment.IsCurrent)
            throw new KnowledgeBundleResolutionException("core_assignment_not_current", "A current Core Knowledge Pack assignment is required before intelligence can be resolved.");
        if (!string.Equals(assignment.PackKey, core.PackKey, StringComparison.Ordinal) ||
            !string.Equals(assignment.ExactVersion, core.ExactVersion, StringComparison.Ordinal))
            throw new KnowledgeBundleResolutionException("core_manifest_unavailable", "The assigned Core Knowledge Pack version does not have a matching packaged Manifest v2.");
    }

    private static IReadOnlyList<ResolvedKnowledgeManifest> ResolveManifests(string categoryKey, KnowledgePackManifestV2 core)
    {
        var manifests = new List<ResolvedKnowledgeManifest> { ResolveManifest(core) };
        var categoryManifests = new[] { RestaurantCafeKnowledgeManifestV2.Create() };
        foreach (var manifest in categoryManifests
                     .Where(item => item.SupportedCategoryKeys.Contains(categoryKey, StringComparer.Ordinal))
                     .OrderBy(item => item.PackKey, StringComparer.Ordinal))
            manifests.Add(ResolveManifest(manifest));
        return manifests;
    }

    private static ResolvedKnowledgeManifest ResolveManifest(KnowledgePackManifestV2 manifest) => new(
        manifest.Layer,
        manifest.PackKey,
        manifest.ExactVersion,
        KnowledgePackManifestV2Policy.Fingerprint(manifest));

    private static string? ResolveSubcategory(Guid businessId, string categoryKey, IReadOnlyCollection<BusinessProfileField> profileFields)
    {
        var candidate = profileFields
            .Where(item => item.BusinessId == businessId && item.OwnerConfirmed &&
                           string.Equals(item.Key, "subcategory", StringComparison.OrdinalIgnoreCase) &&
                           !string.IsNullOrWhiteSpace(item.Value))
            .OrderByDescending(item => item.UpdatedAt)
            .ThenBy(item => item.Id)
            .Select(item => item.Value.Trim().ToLowerInvariant())
            .FirstOrDefault();

        return candidate is not null && BusinessCategoryTaxonomy.IsKnownSubcategory(categoryKey, candidate) ? candidate : null;
    }

    private static IReadOnlyList<ResolvedKnowledgeFact> ResolveContext(Guid businessId, IReadOnlyCollection<BusinessContextEntry> contextEntries) =>
        contextEntries
            .Where(item => item.BusinessId == businessId && item.OwnerConfirmed && !string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => new ResolvedKnowledgeFact(
                KnowledgeEvidenceLayers.Context,
                item.Key.Trim(),
                item.Value.Trim(),
                string.IsNullOrWhiteSpace(item.Source) ? FieldSources.Owner : item.Source.Trim()))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ThenBy(item => item.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Source, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<ResolvedKnowledgeFact> ResolveLocalMarket(Business business) =>
    [
        new(KnowledgeEvidenceLayers.LocalMarket, "country", business.Country.Trim(), "business-record"),
        new(KnowledgeEvidenceLayers.LocalMarket, "timezone", business.Timezone.Trim(), "business-record"),
        new(KnowledgeEvidenceLayers.LocalMarket, "currency", business.Currency.Trim(), "business-record"),
        new(KnowledgeEvidenceLayers.LocalMarket, "primaryLocation", business.PrimaryLocation.Trim(), "business-record")
    ];

    private static IReadOnlyList<ResolvedKnowledgeFact> ResolveMemory(Guid businessId, IReadOnlyCollection<BusinessMemoryItem> memoryItems) =>
        memoryItems
            .Where(item => item.BusinessId == businessId && !string.IsNullOrWhiteSpace(item.StableKey) && !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => new ResolvedKnowledgeFact(
                KnowledgeEvidenceLayers.Memory,
                item.StableKey.Trim(),
                item.Value.Trim(),
                string.IsNullOrWhiteSpace(item.SourceType) ? "memory" : item.SourceType.Trim()))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ThenBy(item => item.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Source, StringComparer.Ordinal)
            .ToArray();

    private static string Fingerprint(
        string categoryKey,
        string? subcategoryKey,
        IReadOnlyList<ResolvedKnowledgeManifest> manifests,
        IReadOnlyList<ResolvedKnowledgeFact> contextFacts,
        IReadOnlyList<ResolvedKnowledgeFact> localMarketFacts,
        IReadOnlyList<ResolvedKnowledgeFact> memoryFacts,
        IReadOnlyList<ResolvedKnowledgeFact> operationalFacts)
    {
        var canonical = new
        {
            CategoryKey = categoryKey,
            SubcategoryKey = subcategoryKey,
            Manifests = manifests.OrderBy(item => item.Layer, StringComparer.Ordinal).ThenBy(item => item.PackKey, StringComparer.Ordinal)
                .Select(item => new { item.Layer, item.PackKey, item.ExactVersion, item.Fingerprint }).ToArray(),
            ContextFacts = CanonicalFacts(contextFacts),
            LocalMarketFacts = CanonicalFacts(localMarketFacts),
            MemoryFacts = CanonicalFacts(memoryFacts),
            OperationalFacts = CanonicalFacts(operationalFacts)
        };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical));
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static object[] CanonicalFacts(IEnumerable<ResolvedKnowledgeFact> facts) =>
        facts.OrderBy(item => item.Key, StringComparer.Ordinal)
            .ThenBy(item => item.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Source, StringComparer.Ordinal)
            .Select(item => (object)new { item.Layer, item.Key, item.Value, item.Source })
            .ToArray();

    private static IReadOnlyList<ResolvedKnowledgeFact> CanonicalOperationalFacts(
        IEnumerable<ResolvedKnowledgeFact> facts) =>
        facts.Where(item => string.Equals(item.Layer, KnowledgeEvidenceLayers.Operational, StringComparison.Ordinal) &&
                            !string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ThenBy(item => item.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Source, StringComparer.Ordinal)
            .ToArray();
}
