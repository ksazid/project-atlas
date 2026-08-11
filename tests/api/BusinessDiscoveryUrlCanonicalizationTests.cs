using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryUrlCanonicalizationTests
{
    [Theory]
    [InlineData("Antalya Kebab St. Julian's - Bolt Food https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians?utm_source=share_provider&utm_medium=product&utm_content=menu_header", "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians")]
    [InlineData("https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86?g_st=ic", "https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86")]
    public void Canonicalizer_StripsShareTextAndTracking(string raw, string expected)
    {
        var ok = PublicBusinessUrlPolicy.TryCanonicalize(raw, out var canonical, out var error);

        Assert.True(ok, error);
        Assert.NotNull(canonical);
        Assert.Equal(expected, canonical!.Value);
    }

    [Theory]
    [InlineData("https://google.com/search?q=antalya+kebab")]
    [InlineData("https://food.bolt.eu/en/324")]
    [InlineData("https://wolt.com/en/mlt")]
    [InlineData("https://127.0.0.1/business")]
    [InlineData("https://[::1]/business")]
    [InlineData("https://user:password@example.com/business")]
    public void Canonicalizer_RejectsGenericOrUnsafeSources(string raw)
    {
        Assert.False(PublicBusinessUrlPolicy.TryCanonicalize(raw, out _, out _));
    }

    [Fact]
    public void Canonicalizer_RejectsAmbiguousShareTextWithMultipleUrls()
    {
        const string raw = "https://example.com/a https://example.com/b";

        Assert.False(PublicBusinessUrlPolicy.TryCanonicalize(raw, out _, out _));
    }

    [Fact]
    public void CanonicalizeMany_RejectsCanonicalDuplicatesBeforeDiscovery()
    {
        var error = Assert.Throws<BusinessDiscoveryException>(() => PublicBusinessUrlPolicy.CanonicalizeMany(
            "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians?utm_source=one",
            ["https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians?utm_medium=two"]));

        Assert.Equal("business_source_duplicate", error.Code);
    }

    [Fact]
    public void CanonicalizeMany_RejectsMoreThanThreeSources()
    {
        var error = Assert.Throws<BusinessDiscoveryException>(() => PublicBusinessUrlPolicy.CanonicalizeMany(
            "https://example.com/business",
            ["https://example.net/business", "https://example.org/business", "https://example.edu/business"]));

        Assert.Equal("business_sources_too_many", error.Code);
    }
}
