using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class KnowledgeBundleResolverTests
{
    [Fact]
    public void Restaurant_cafe_automatically_resolves_core_and_category_manifests()
    {
        var business = BusinessRecord("restaurant-cafe");
        var bundle = KnowledgeBundleResolver.Resolve(business, CoreAssignment(business.Id), [], [], []);

        Assert.Equal([KnowledgePackLayers.Core, KnowledgePackLayers.Category], bundle.Manifests.Select(x => x.Layer));
        Assert.Equal([KnowledgePackKeys.GenericBusiness, RestaurantCafeKnowledgeManifestV2.PackKey], bundle.Manifests.Select(x => x.PackKey));
        Assert.All(bundle.Manifests, item => Assert.Equal(64, item.Fingerprint.Length));
        Assert.Equal("restaurant-cafe", bundle.CategoryKey);
    }

    [Fact]
    public void Category_without_pack_resolves_core_only()
    {
        var business = BusinessRecord("retail");
        var bundle = KnowledgeBundleResolver.Resolve(business, CoreAssignment(business.Id), [], [], []);

        var manifest = Assert.Single(bundle.Manifests);
        Assert.Equal(KnowledgePackLayers.Core, manifest.Layer);
        Assert.Equal(KnowledgePackKeys.GenericBusiness, manifest.PackKey);
    }

    [Fact]
    public void Subcategory_requires_owner_confirmed_canonical_profile_evidence()
    {
        var business = BusinessRecord("restaurant-cafe");
        var confirmed = ProfileField(business.Id, "subcategory", "cafe", ownerConfirmed: true);
        var unconfirmed = ProfileField(business.Id, "subcategory", "bakery", ownerConfirmed: false);

        Assert.Equal("cafe", KnowledgeBundleResolver.Resolve(business, CoreAssignment(business.Id), [confirmed], [], []).SubcategoryKey);
        Assert.Null(KnowledgeBundleResolver.Resolve(business, CoreAssignment(business.Id), [unconfirmed], [], []).SubcategoryKey);

        var unknown = ProfileField(business.Id, "subcategory", "unknown-type", ownerConfirmed: true);
        Assert.Null(KnowledgeBundleResolver.Resolve(business, CoreAssignment(business.Id), [unknown], [], []).SubcategoryKey);
    }

    [Fact]
    public void Context_is_owner_confirmed_and_local_market_and_memory_are_separate_layers()
    {
        var business = BusinessRecord("restaurant-cafe");
        var context = new[]
        {
            Context(business.Id, "primarychannels", "Dine in", true),
            Context(business.Id, "constraints", "Staffing", false),
            Context(business.Id, "busyperiods", " ", true)
        };
        var memory = new[] { Memory(business.Id, "outcome:1", "owner-reported: customers asked about takeaway") };

        var bundle = KnowledgeBundleResolver.Resolve(business, CoreAssignment(business.Id), [], context, memory);

        var contextFact = Assert.Single(bundle.ContextFacts);
        Assert.Equal("primarychannels", contextFact.Key);
        Assert.Equal(4, bundle.LocalMarketFacts.Count);
        Assert.Contains(bundle.LocalMarketFacts, x => x.Key == "country" && x.Value == "MT");
        Assert.Contains(bundle.LocalMarketFacts, x => x.Key == "timezone" && x.Value == "Europe/Malta");
        Assert.Contains(bundle.LocalMarketFacts, x => x.Key == "currency" && x.Value == "EUR");
        Assert.Contains(bundle.LocalMarketFacts, x => x.Key == "primaryLocation" && x.Value == "Valletta");
        Assert.Equal("outcome:1", Assert.Single(bundle.MemoryFacts).Key);
    }

    [Fact]
    public void Semantic_fingerprint_is_stable_across_input_collection_order()
    {
        var business = BusinessRecord("restaurant-cafe");
        var assignment = CoreAssignment(business.Id);
        var context = new[]
        {
            Context(business.Id, "primarychannels", "Dine in", true),
            Context(business.Id, "currentpriorities", "Improve weekday lunch", true)
        };
        var memory = new[]
        {
            Memory(business.Id, "outcome:1", "first"),
            Memory(business.Id, "outcome:2", "second")
        };

        var first = KnowledgeBundleResolver.Resolve(business, assignment, [], context, memory);
        var second = KnowledgeBundleResolver.Resolve(business, assignment, [], context.Reverse().ToArray(), memory.Reverse().ToArray());

        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    [Fact]
    public void Exact_core_assignment_is_required_for_reproducible_resolution()
    {
        var business = BusinessRecord("restaurant-cafe");
        var wrongVersion = CoreAssignment(business.Id);
        wrongVersion.ExactVersion = "9.9";
        var wrongPack = CoreAssignment(business.Id);
        wrongPack.PackKey = "other-core";
        var notCurrent = CoreAssignment(business.Id);
        notCurrent.IsCurrent = false;

        Assert.Throws<KnowledgeBundleResolutionException>(() => KnowledgeBundleResolver.Resolve(business, wrongVersion, [], [], []));
        Assert.Throws<KnowledgeBundleResolutionException>(() => KnowledgeBundleResolver.Resolve(business, wrongPack, [], [], []));
        Assert.Throws<KnowledgeBundleResolutionException>(() => KnowledgeBundleResolver.Resolve(business, notCurrent, [], [], []));
    }

    private static Business BusinessRecord(string category) => new()
    {
        Id = Guid.NewGuid(), Name = "Reference Business", Category = category, Country = "MT", Timezone = "Europe/Malta",
        Currency = "EUR", PrimaryLocation = "Valletta", OperatingStatus = "open", CreatedAt = DateTimeOffset.UtcNow
    };

    private static BusinessKnowledgeAssignment CoreAssignment(Guid businessId) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, KnowledgePackId = Guid.NewGuid(), KnowledgePackVersionId = Guid.NewGuid(),
        PackKey = KnowledgePackKeys.GenericBusiness, ExactVersion = GenericBusinessKnowledgePack.InitialVersion,
        IsCurrent = true, AssignedByUserAccountId = Guid.NewGuid(), AssignedAt = DateTimeOffset.UtcNow, EffectiveAt = DateTimeOffset.UtcNow
    };

    private static BusinessProfileField ProfileField(Guid businessId, string key, string value, bool ownerConfirmed) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, Key = key, Value = value, Source = FieldSources.Owner,
        EvidenceClass = "owner-reported", OwnerConfirmed = ownerConfirmed, UpdatedAt = DateTimeOffset.UtcNow
    };

    private static BusinessContextEntry Context(Guid businessId, string key, string value, bool ownerConfirmed) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, Key = key, Value = value, Source = FieldSources.Owner,
        OwnerConfirmed = ownerConfirmed, UpdatedAt = DateTimeOffset.UtcNow
    };

    private static BusinessMemoryItem Memory(Guid businessId, string key, string value) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, StableKey = key, Category = BusinessMemoryCategories.Outcome,
        SourceType = "outcome", Value = value, IsDeletable = true, CapturedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
    };
}
