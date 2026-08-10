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
