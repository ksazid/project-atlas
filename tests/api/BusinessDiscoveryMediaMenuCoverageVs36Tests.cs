using System.Reflection;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryMediaMenuCoverageVs36Tests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Coverage_contract_exposes_the_six_provider_neutral_states()
    {
        var type = typeof(PublicBusinessMediaMenuExtractor).Assembly.GetType("Atlas.Api.PublicBusinessMediaMenuCoverage");
        Assert.NotNull(type);

        Assert.Equal("structured", Constant(type, "Structured"));
        Assert.Equal("semantic-html", Constant(type, "SemanticHtml"));
        Assert.Equal("embedded-public-state", Constant(type, "EmbeddedPublicState"));
        Assert.Equal("media-only", Constant(type, "MediaOnly"));
        Assert.Equal("renderer-required", Constant(type, "RendererRequired"));
        Assert.Equal("none", Constant(type, "None"));
    }

    [Fact]
    public void Extractor_distinguishes_structured_semantic_media_renderer_and_none()
    {
        var website = new Uri("https://restaurant.example.com/");
        var bolt = new Uri("https://food.bolt.eu/en/324-valletta/p/1257-chickn-bites/");

        var structured = PublicBusinessMediaMenuExtractor.Extract(
            "website",
            website,
            """<script type="application/ld+json">{"@type":"Restaurant","name":"Atlas Kitchen","hasMenu":{"@type":"Menu","hasMenuItem":{"@type":"MenuItem","name":"Kebab"}}}</script>""",
            Now);
        Assert.Equal("structured", Coverage(structured));

        var semantic = PublicBusinessMediaMenuExtractor.Extract(
            "bolt-food",
            bolt,
            """<h2 class="provider-menu-category-title">Mains</h2><ul><li class="provider-menu-dish"><img alt="Kebab" src="https://images.bolt.eu/kebab.jpg"><span class="provider-menu-dish-price">€9.50</span></li></ul>""",
            Now);
        Assert.Equal("semantic-html", Coverage(semantic));

        var mediaOnly = PublicBusinessMediaMenuExtractor.Extract(
            "website",
            website,
            """<meta property="og:image" content="https://cdn.example.com/cover.jpg">""",
            Now);
        Assert.Equal("media-only", Coverage(mediaOnly));

        var renderer = PublicBusinessMediaMenuExtractor.Extract(
            "bolt-food",
            bolt,
            """<html><body>Oh no! It looks like JavaScript is not enabled in your browser.</body></html>""",
            Now);
        Assert.Equal("renderer-required", Coverage(renderer));
        Assert.Empty(renderer.Media);
        Assert.Empty(renderer.Offerings);

        var none = PublicBusinessMediaMenuExtractor.Extract(
            "website",
            website,
            "<html><body>Open daily</body></html>",
            Now);
        Assert.Equal("none", Coverage(none));
    }

    [Fact]
    public void Public_business_snapshot_carries_coverage_without_turning_it_into_a_fact()
    {
        var snapshot = PublicBusinessExtractor.Extract(
            "bolt-food",
            new Uri("https://food.bolt.eu/en/324-valletta/p/1257-chickn-bites/"),
            """<html><head><title>Chick'n Bites | Bolt Food</title></head><body>JavaScript is not enabled in your browser.</body></html>""",
            Now);

        var property = typeof(PublicBusinessSnapshot).GetProperty("MediaMenuCoverage", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        Assert.Equal("renderer-required", property.GetValue(snapshot));
        Assert.DoesNotContain(snapshot.Facts, fact => fact.Key.Contains("coverage", StringComparison.OrdinalIgnoreCase));
    }

    private static string Coverage(PublicBusinessMediaMenuExtraction extraction)
    {
        var property = typeof(PublicBusinessMediaMenuExtraction).GetProperty("Coverage", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        return Assert.IsType<string>(property.GetValue(extraction));
    }

    private static string Constant(Type type, string name)
    {
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(field);
        return Assert.IsType<string>(field.GetRawConstantValue());
    }
}
