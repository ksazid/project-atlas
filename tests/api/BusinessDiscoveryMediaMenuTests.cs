using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryMediaMenuTests
{
    [Fact]
    public void Extractor_captures_public_images_and_structured_menu_items_with_provenance()
    {
        var observedAt = new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
        var source = new Uri("https://restaurant.example.com/menu");
        const string html = """
            <html><head>
              <meta property="og:image" content="https://cdn.example.com/fallback.jpg">
              <script type="application/ld+json">
              {
                "@context": "https://schema.org",
                "@type": "Restaurant",
                "name": "Atlas Kitchen",
                "image": [
                  "https://cdn.example.com/hero.jpg",
                  {"@type":"ImageObject","contentUrl":"https://cdn.example.com/food.jpg"}
                ],
                "hasMenu": {
                  "@type": "Menu",
                  "url": "https://restaurant.example.com/menu/current",
                  "hasMenuSection": [{
                    "@type": "MenuSection",
                    "name": "Kebabs",
                    "hasMenuItem": [{
                      "@type": "MenuItem",
                      "name": "Chicken Kebab",
                      "description": "Chargrilled chicken with salad",
                      "offers": {"@type":"Offer","price":"12.50","priceCurrency":"EUR"}
                    }]
                  }]
                }
              }
              </script>
            </head></html>
            """;

        var snapshot = PublicBusinessExtractor.Extract("website", source, html, observedAt);

        Assert.Equal(2, snapshot.Media.Count);
        Assert.Contains(snapshot.Media, x => x.RemoteUrl == "https://cdn.example.com/hero.jpg" && x.Kind == "business-image");
        Assert.Contains(snapshot.Media, x => x.RemoteUrl == "https://cdn.example.com/food.jpg" && x.SourceUrl == source.ToString());
        Assert.DoesNotContain(snapshot.Media, x => x.RemoteUrl.Contains("fallback.jpg", StringComparison.Ordinal));

        var item = Assert.Single(snapshot.Offerings);
        Assert.Equal("menu-item", item.Kind);
        Assert.Equal("Kebabs", item.Section);
        Assert.Equal("Chicken Kebab", item.Name);
        Assert.Equal("Chargrilled chicken with salad", item.Description);
        Assert.Equal(12.50m, item.Price);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal("website", item.Source);
        Assert.Equal(source.ToString(), item.SourceUrl);
        Assert.False(item.OwnerConfirmed);

        Assert.Contains(snapshot.Facts, x => x.Key == "menuUrl" && x.Value == "https://restaurant.example.com/menu/current");
    }

    [Fact]
    public void Bolt_public_ssr_menu_is_extracted_from_provider_semantic_markup_without_private_api_access()
    {
        var observedAt = new DateTimeOffset(2026, 8, 11, 14, 0, 0, TimeSpan.Zero);
        var source = new Uri("https://food.bolt.eu/en/324-valletta/p/1310-hasans-turkish-kebab-house/");
        const string html = """
            <html><head>
              <title>Hasan's Turkish Kebab House | Bolt Food</title>
              <meta property="og:image" content="https://images.bolt.eu/store/hasan-cover.jpg">
            </head><body>
              <h1 class="provider-name">Hasan's Turkish Kebab House</h1>
              <h2 class="provider-menu-category-title">Beverages</h2>
              <ul>
                <li class="provider-menu-dish">
                  <img src="https://images.bolt.eu/store/ice-tea.jpg" alt="Ice Tea Peach">
                  <div class="provider-menu-dish-content">
                    <p class="provider-menu-dish-description">Incl. €0.10 deposit.</p>
                    <span class="provider-menu-dish-price">€2.50</span>
                  </div>
                </li>
              </ul>
              <h2 class="provider-menu-category-title">Wraps &amp; Pita</h2>
              <ul>
                <li class="provider-menu-dish featured">
                  <img alt="Any Grill in Pita Bread" src="https://images.bolt.eu/store/grill-pita.jpg">
                  <div class="provider-menu-dish-content">
                    <p class="provider-menu-dish-description">Tasty grilled pita bread filled with your choice of meat and fresh salad.</p>
                    <span class="provider-menu-dish-price">€9.50</span>
                  </div>
                </li>
              </ul>
            </body></html>
            """;

        var snapshot = PublicBusinessExtractor.Extract("bolt-food", source, html, observedAt);

        Assert.Equal(2, snapshot.Offerings.Count);
        Assert.Contains(snapshot.Offerings, item =>
            item.Section == "Beverages" &&
            item.Name == "Ice Tea Peach" &&
            item.Description == "Incl. €0.10 deposit." &&
            item.Price == 2.50m &&
            item.Currency == "EUR" &&
            item.Source == "bolt-food" &&
            !item.OwnerConfirmed);
        Assert.Contains(snapshot.Offerings, item =>
            item.Section == "Wraps & Pita" &&
            item.Name == "Any Grill in Pita Bread" &&
            item.Price == 9.50m &&
            item.Currency == "EUR");

        Assert.Contains(snapshot.Media, item =>
            item.Kind == "menu-item-image" &&
            item.RemoteUrl == "https://images.bolt.eu/store/ice-tea.jpg" &&
            item.AltText == "Ice Tea Peach" &&
            item.Source == "bolt-food");
        Assert.Contains(snapshot.Media, item =>
            item.Kind == "menu-item-image" &&
            item.RemoteUrl == "https://images.bolt.eu/store/grill-pita.jpg" &&
            item.AltText == "Any Grill in Pita Bread");
    }

    [Fact]
    public void Extractor_uses_safe_og_image_fallback_and_rejects_non_https_media()
    {
        var observedAt = DateTimeOffset.UtcNow;
        var source = new Uri("https://restaurant.example.com/");
        const string safeHtml = """
            <html><head>
              <meta property="og:image" content="https://cdn.example.com/cover.jpg">
              <script type="application/ld+json">{"@context":"https://schema.org","@type":"Restaurant","name":"Atlas Kitchen"}</script>
            </head></html>
            """;
        const string unsafeHtml = """
            <html><head>
              <meta property="og:image" content="http://cdn.example.com/cover.jpg">
              <script type="application/ld+json">{"@context":"https://schema.org","@type":"Restaurant","name":"Atlas Kitchen"}</script>
            </head></html>
            """;

        var safe = PublicBusinessExtractor.Extract("website", source, safeHtml, observedAt);
        var unsafeSnapshot = PublicBusinessExtractor.Extract("website", source, unsafeHtml, observedAt);

        var image = Assert.Single(safe.Media);
        Assert.Equal("https://cdn.example.com/cover.jpg", image.RemoteUrl);
        Assert.Equal("business-image", image.Kind);
        Assert.Empty(unsafeSnapshot.Media);
    }

    [Fact]
    public void Reconciliation_excludes_media_and_menu_from_mismatched_secondary_business()
    {
        var now = DateTimeOffset.UtcNow;
        var anchor = new BusinessSourceObservation(
            0,
            true,
            "website",
            "https://atlas-kitchen.example.com/",
            "success",
            [Fact("name", "Atlas Kitchen", "website", "https://atlas-kitchen.example.com/", now)],
            Media: [new PublicBusinessMedia("business-image", "https://cdn.example.com/atlas.jpg", "website", "https://atlas-kitchen.example.com/", now, "high")],
            Offerings: [new PublicBusinessOffering("menu-item", "Mains", "Kebab", null, 10m, "EUR", "website", "https://atlas-kitchen.example.com/", now, "high")]);
        var mismatch = new BusinessSourceObservation(
            1,
            false,
            "website",
            "https://other-cafe.example.com/",
            "success",
            [Fact("name", "Other Cafe", "website", "https://other-cafe.example.com/", now)],
            Media: [new PublicBusinessMedia("business-image", "https://cdn.example.com/other.jpg", "website", "https://other-cafe.example.com/", now, "high")],
            Offerings: [new PublicBusinessOffering("menu-item", "Coffee", "Latte", null, 4m, "EUR", "website", "https://other-cafe.example.com/", now, "high")]);

        var result = BusinessDiscoveryReconciler.Reconcile([anchor, mismatch]);

        var media = Assert.Single(result.Snapshot.Media);
        Assert.Equal("https://cdn.example.com/atlas.jpg", media.RemoteUrl);
        var offering = Assert.Single(result.Snapshot.Offerings);
        Assert.Equal("Kebab", offering.Name);
        Assert.Contains("business_source_identity_mismatch", result.Warnings);
    }

    private static PublicBusinessFact Fact(string key, string value, string source, string sourceUrl, DateTimeOffset observedAt) =>
        new(key, value, source, sourceUrl, observedAt, "high");
}