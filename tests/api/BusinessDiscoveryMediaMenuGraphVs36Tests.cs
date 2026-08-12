using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryMediaMenuGraphVs36Tests
{
    [Fact]
    public void Structured_graph_resolves_menu_references_and_menu_item_images()
    {
        var now = new DateTimeOffset(2026, 8, 12, 20, 15, 0, TimeSpan.Zero);
        var source = new Uri("https://restaurant.example.com/");
        const string html = """
          <script type="application/ld+json">
          {
            "@context":"https://schema.org",
            "@graph":[
              {"@type":"Restaurant","@id":"#business","name":"Atlas Kitchen","hasMenu":{"@id":"#menu"}},
              {"@type":"Menu","@id":"#menu","hasMenuSection":{"@id":"#mains"}},
              {"@type":"MenuSection","@id":"#mains","name":"Mains","hasMenuItem":{"@id":"#kebab"}},
              {"@type":"MenuItem","@id":"#kebab","name":"Chicken Kebab","description":"Chargrilled chicken","image":{"@type":"ImageObject","contentUrl":"https://cdn.example.com/kebab.jpg"},"offers":{"@type":"Offer","price":"12.50","priceCurrency":"EUR"}}
            ]
          }
          </script>
          """;

        var result = PublicBusinessMediaMenuExtractor.Extract("website", source, html, now);

        var offering = Assert.Single(result.Offerings);
        Assert.Equal("Mains", offering.Section);
        Assert.Equal("Chicken Kebab", offering.Name);
        Assert.Equal(12.50m, offering.Price);
        Assert.Equal("EUR", offering.Currency);
        Assert.Contains(result.Media, media =>
            media.Kind == "menu-item-image" &&
            media.RemoteUrl == "https://cdn.example.com/kebab.jpg" &&
            media.AltText == "Chicken Kebab");
        Assert.Equal(PublicBusinessMediaMenuCoverage.Structured, result.Coverage);
    }

    [Fact]
    public void Structured_graph_rejects_non_https_menu_item_images_but_keeps_the_offering()
    {
        var now = new DateTimeOffset(2026, 8, 12, 20, 20, 0, TimeSpan.Zero);
        var source = new Uri("https://restaurant.example.com/");
        const string html = """
          <script type="application/ld+json">
          {
            "@context":"https://schema.org",
            "@graph":[
              {"@type":"Restaurant","name":"Atlas Kitchen","hasMenu":{"@id":"#menu"}},
              {"@type":"Menu","@id":"#menu","hasMenuItem":{"@id":"#soup"}},
              {"@type":"MenuItem","@id":"#soup","name":"Soup","image":"http://cdn.example.com/soup.jpg"}
            ]
          }
          </script>
          """;

        var result = PublicBusinessMediaMenuExtractor.Extract("website", source, html, now);

        Assert.Equal("Soup", Assert.Single(result.Offerings).Name);
        Assert.DoesNotContain(result.Media, media => media.RemoteUrl.Contains("soup.jpg", StringComparison.Ordinal));
        Assert.Equal(PublicBusinessMediaMenuCoverage.Structured, result.Coverage);
    }
}