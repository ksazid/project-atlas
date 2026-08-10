using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessLocationResolutionTests
{
    [Fact]
    public void MarketplaceIdentity_UsesMerchantSlug_WhenBoltMetadataIsProviderGeneric()
    {
        var finalUri = new Uri("https://food.bolt.eu/en/324-valletta/p/11881-gun-turkish-kebab/");

        var name = MarketplaceBusinessIdentity.ResolveName("bolt-food", finalUri, "Bolt Food");

        Assert.Equal("Gun Turkish Kebab", name.Value);
        Assert.Equal("medium", name.Confidence);
    }

    [Fact]
    public void MarketplaceIdentity_PreservesMerchantSpecificMetadata()
    {
        var finalUri = new Uri("https://food.bolt.eu/en/324-valletta/p/11881-gun-turkish-kebab/");

        var name = MarketplaceBusinessIdentity.ResolveName("bolt-food", finalUri, "GÜN Turkish Kebab");

        Assert.Equal("GÜN Turkish Kebab", name.Value);
        Assert.Equal("high", name.Confidence);
    }

    [Fact]
    public void DiscoverySnapshot_ReplacesGenericBoltIdentityWithMerchantSlug()
    {
        var observedAt = new DateTimeOffset(2026, 8, 10, 18, 0, 0, TimeSpan.Zero);
        var sourceUrl = "https://food.bolt.eu/en/324-valletta/p/11881-gun-turkish-kebab/";
        var publicSnapshot = new PublicBusinessSnapshot(
            "bolt-food",
            sourceUrl,
            observedAt,
            [
                new PublicBusinessFact("name", "Bolt Food", "bolt-food", sourceUrl, observedAt, "medium"),
                new PublicBusinessFact("category", "restaurant-cafe", "bolt-food", sourceUrl, observedAt, "high")
            ]);

        var snapshot = BusinessDiscoverySnapshot.Create(Guid.NewGuid(), publicSnapshot);

        var name = snapshot.Facts.Single(x => x.Key == "name");
        Assert.Equal("Gun Turkish Kebab", name.Value);
        Assert.Equal("medium", name.Confidence);
    }

    [Fact]
    public void MarketMetadata_NormalizesMaltaLocationWithoutOwnerTypingTechnicalCodes()
    {
        var metadata = BusinessMarketMetadata.Resolve("MT", "Europe/Malta");

        Assert.Equal("Malta", metadata.CountryName);
        Assert.Equal("MT", metadata.CountryCode);
        Assert.Equal("Europe/Malta", metadata.Timezone);
        Assert.Equal("EUR", metadata.Currency);
        Assert.Equal("€", metadata.CurrencySymbol);
    }

    [Fact]
    public void ConfirmationValidation_RejectsOwnerTypedNonCanonicalMarketMetadata()
    {
        var request = new CreateBusinessFromDiscoveryRequest(
            Guid.NewGuid(), "Gun Turkish Kebab", "restaurant-cafe", "restaurant",
            "Malta", "Malta", "Euro", "Birkirkara, Malta", "Open",
            null, null, null, null, "English", true);

        var errors = request.Validate();

        Assert.Contains(nameof(request.Country), errors.Keys);
        Assert.Contains(nameof(request.Currency), errors.Keys);
    }

    [Fact]
    public void LocationSelection_RequiresExplicitChoice_WhenSeveralBranchesMatch()
    {
        var result = BusinessLocationResolution.Classify(
        [
            new BusinessLocationCandidate("place-1", "POSH Turkish — Sliema", "Sliema, Malta", 35.91, 14.50, "MT", "Malta", "Europe/Malta", "EUR", "google-places"),
            new BusinessLocationCandidate("place-2", "POSH Turkish — Valletta", "Valletta, Malta", 35.90, 14.51, "MT", "Malta", "Europe/Malta", "EUR", "google-places")
        ]);

        Assert.Equal(BusinessLocationResolutionState.RequiresSelection, result.State);
        Assert.Null(result.Selected);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void LocationSelection_PreselectsOneStrongCandidate_ButKeepsItChangeable()
    {
        var candidate = new BusinessLocationCandidate("place-1", "GUN Turkish Kebab", "65 Triq Il-Herba, Birkirkara, Malta", 35.90, 14.46, "MT", "Malta", "Europe/Malta", "EUR", "google-places");

        var result = BusinessLocationResolution.Classify([candidate]);

        Assert.Equal(BusinessLocationResolutionState.Preselected, result.State);
        Assert.Equal(candidate, result.Selected);
        Assert.True(result.CanChange);
    }

    [Fact]
    public void LocationSelection_OffersSearch_WhenNoReliableCandidateExists()
    {
        var result = BusinessLocationResolution.Classify([]);

        Assert.Equal(BusinessLocationResolutionState.SearchRequired, result.State);
        Assert.Empty(result.Candidates);
        Assert.Null(result.Selected);
    }
}
