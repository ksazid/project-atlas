using System.Net;
using System.Text;
using Atlas.Api;
using Microsoft.Extensions.Configuration;
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
    public void MarketplaceContent_DropsOrderingBoilerplate_ButPreservesUsefulDescription()
    {
        Assert.Null(MarketplaceBusinessContent.CleanDescription(
            "bolt-food",
            "Open Antalya Kebab St. Julian's on Bolt Food app to order delivery or pickup."));

        Assert.Equal(
            "Family-run Turkish restaurant serving charcoal-grilled kebabs.",
            MarketplaceBusinessContent.CleanDescription(
                "bolt-food",
                "Family-run Turkish restaurant serving charcoal-grilled kebabs."));
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
    public async Task GoogleLocationProvider_UsesDocumentedTimezoneObject_AndSpecificPlaceTypes_InOneProviderRequest()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("places.googleapis.com", request.RequestUri?.Host);
            Assert.True(request.Headers.TryGetValues("X-Goog-FieldMask", out var fieldMasks));
            var fieldMask = Assert.Single(fieldMasks);
            Assert.Contains("places.timeZone", fieldMask);
            Assert.Contains("places.types", fieldMask);

            const string body = """
            {
              "places": [
                {
                  "id": "place-gun-birkirkara",
                  "displayName": { "text": "GÜN Turkish Kebab" },
                  "formattedAddress": "65 Triq Il-Herba, Birkirkara, Malta",
                  "location": { "latitude": 35.90, "longitude": 14.46 },
                  "addressComponents": [
                    { "shortText": "MT", "types": ["country"] }
                  ],
                  "timeZone": { "id": "Europe/Malta", "version": "2026a" },
                  "types": ["turkish_restaurant", "kebab_shop", "restaurant", "food"]
                }
              ]
            }
            """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });
        using var client = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["GoogleMaps:ApiKey"] = "test-key" })
            .Build();
        var provider = new GoogleBusinessLocationProvider(client, configuration);

        var candidates = await provider.SearchAsync("GUN Turkish Kebab Malta", CancellationToken.None);

        var candidate = Assert.Single(candidates);
        Assert.Equal("Europe/Malta", candidate.Timezone);
        Assert.Equal("MT", candidate.CountryCode);
        Assert.Equal("EUR", candidate.Currency);
        Assert.Equal("Turkish · Kebab", candidate.BusinessTypeSummary);
        Assert.Equal(1, handler.RequestCount);
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

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responder(request));
        }
    }
}