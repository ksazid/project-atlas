using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryMarketplaceIsolationTests
{
    [Fact]
    public void Wolt_extractor_prefers_structured_business_matching_the_source_location()
    {
        const string html = """
            <html><head>
            <meta property="og:title" content="Target Kebab | Wolt" />
            <script type="application/ld+json">
            {
              "@context":"https://schema.org",
              "@graph":[
                {"@type":"Restaurant","name":"Unrelated Pizza","url":"https://wolt.com/en/mlt/malta/restaurant/unrelated-pizza","telephone":"+356 1111 1111"},
                {"@type":"Restaurant","name":"Target Kebab","url":"https://wolt.com/en/mlt/malta/restaurant/target-kebab","telephone":"+356 2222 2222"}
              ]
            }
            </script>
            </head></html>
            """;

        var snapshot = PublicBusinessExtractor.Extract(
            "wolt",
            new Uri("https://wolt.com/en/mlt/malta/restaurant/target-kebab"),
            html,
            DateTimeOffset.UtcNow);

        Assert.Equal("Target Kebab", snapshot.Facts.Single(x => x.Key == "name").Value);
        Assert.Equal("+356 2222 2222", snapshot.Facts.Single(x => x.Key == "phone").Value);
        Assert.DoesNotContain(snapshot.Facts, x => x.Value.Contains("Unrelated Pizza", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Marketplace_extractor_uses_page_metadata_when_structured_merchants_do_not_match_source()
    {
        const string html = """
            <html><head>
            <meta property="og:title" content="Target Kebab | Wolt" />
            <meta property="og:description" content="Target merchant page" />
            <script type="application/ld+json">
            {
              "@context":"https://schema.org",
              "@graph":[
                {"@type":"Restaurant","name":"Unrelated Pizza","url":"https://wolt.com/en/mlt/malta/restaurant/unrelated-pizza","telephone":"+356 1111 1111"},
                {"@type":"Restaurant","name":"Another Merchant","url":"https://wolt.com/en/mlt/malta/restaurant/another-merchant","telephone":"+356 3333 3333"}
              ]
            }
            </script>
            </head></html>
            """;

        var snapshot = PublicBusinessExtractor.Extract(
            "wolt",
            new Uri("https://wolt.com/en/mlt/malta/restaurant/target-kebab"),
            html,
            DateTimeOffset.UtcNow);

        Assert.Equal("Target Kebab", snapshot.Facts.Single(x => x.Key == "name").Value);
        Assert.DoesNotContain(snapshot.Facts, x => x.Key == "phone");
        Assert.DoesNotContain(snapshot.Facts, x => x.Value.Contains("Unrelated", StringComparison.OrdinalIgnoreCase) || x.Value.Contains("Another Merchant", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Bolt_extractor_uses_validated_business_url_identity_when_page_title_is_generic_provider_branding()
    {
        const string html = """
            <html><head>
            <meta property="og:title" content="Bolt Food" />
            <meta property="og:description" content="Order food delivery and takeaway with Bolt Food" />
            <title>Bolt Food</title>
            </head></html>
            """;

        var snapshot = PublicBusinessExtractor.Extract(
            "bolt-food",
            new Uri("https://food.bolt.eu/en/324/p/11881-gun-turkish-kebab"),
            html,
            DateTimeOffset.UtcNow);

        var name = snapshot.Facts.Single(x => x.Key == "name");
        Assert.Equal("Gun Turkish Kebab", name.Value);
        Assert.Equal("medium", name.Confidence);
        Assert.DoesNotContain(snapshot.Facts, fact => fact.Key == "name" && string.Equals(fact.Value, "Bolt Food", StringComparison.OrdinalIgnoreCase));
    }
}
