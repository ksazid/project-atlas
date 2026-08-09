using System.Net;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryPolicyTests
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://user:pass@example.com")]
    [InlineData("https://example.com:8443")]
    [InlineData("https://localhost")]
    [InlineData("https://localhost.localdomain")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://10.0.0.8")]
    [InlineData("https://172.16.0.8")]
    [InlineData("https://192.168.1.8")]
    [InlineData("https://169.254.10.20")]
    [InlineData("https://100.64.0.1")]
    [InlineData("https://192.0.2.10")]
    [InlineData("https://198.51.100.10")]
    [InlineData("https://203.0.113.10")]
    [InlineData("https://[::1]")]
    [InlineData("https://[fc00::1]")]
    [InlineData("https://[fe80::1]")]
    public void UrlPolicy_RejectsUnsafeTargets(string value)
    {
        Assert.False(PublicBusinessUrlPolicy.TryValidate(value, out _, out _));
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://www.example.com/menu")]
    public void UrlPolicy_AcceptsNormalHttpsBusinessUrls(string value)
    {
        Assert.True(PublicBusinessUrlPolicy.TryValidate(value, out var uri, out var error));
        Assert.NotNull(uri);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.10.1.5", false)]
    [InlineData("172.31.255.5", false)]
    [InlineData("192.168.50.1", false)]
    [InlineData("169.254.1.2", false)]
    [InlineData("100.64.2.3", false)]
    [InlineData("192.0.2.2", false)]
    [InlineData("198.51.100.3", false)]
    [InlineData("203.0.113.4", false)]
    [InlineData("8.8.8.8", true)]
    [InlineData("1.1.1.1", true)]
    [InlineData("::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("2001:4860:4860::8888", true)]
    public void UrlPolicy_ClassifiesResolvedAddresses(string value, bool expected)
    {
        Assert.Equal(expected, PublicBusinessUrlPolicy.IsPublicAddress(IPAddress.Parse(value)));
    }

    [Fact]
    public async Task HtmlReader_RejectsOversizedResponses()
    {
        using var content = new StringContent(new string('x', PublicBusinessHtmlReader.MaxCharacters + 1));

        var error = await Assert.ThrowsAsync<BusinessDiscoveryException>(() => PublicBusinessHtmlReader.ReadAsync(content, CancellationToken.None));

        Assert.Equal("business_source_too_large", error.Code);
    }

    [Fact]
    public void Extractor_PrefersStructuredLocalBusinessFacts_AndLeavesMissingFactsUnknown()
    {
        const string html = """
            <html><head>
            <title>Fallback Name</title>
            <meta property="og:title" content="OG Name" />
            <meta property="og:description" content="OG description" />
            <script type="application/ld+json">
            {
              "@context":"https://schema.org",
              "@type":"CafeOrCoffeeShop",
              "name":"Harbour Coffee",
              "description":"Independent coffee shop and bakery",
              "telephone":"+356 2100 0000",
              "url":"https://harbour.example",
              "address":{"@type":"PostalAddress","streetAddress":"1 Republic Street","addressLocality":"Valletta","addressCountry":"MT"},
              "openingHours":["Mo-Fr 07:00-18:00","Sa-Su 08:00-16:00"]
            }
            </script>
            </head><body></body></html>
            """;
        var observedAt = new DateTimeOffset(2026, 8, 9, 19, 30, 0, TimeSpan.Zero);

        var snapshot = PublicBusinessExtractor.Extract("website", new Uri("https://harbour.example"), html, observedAt);

        Assert.Equal("Harbour Coffee", snapshot.Facts.Single(x => x.Key == "name").Value);
        Assert.Equal("restaurant-cafe", snapshot.Facts.Single(x => x.Key == "category").Value);
        Assert.Equal("cafe", snapshot.Facts.Single(x => x.Key == "subcategory").Value);
        Assert.Equal("1 Republic Street, Valletta, MT", snapshot.Facts.Single(x => x.Key == "primaryLocation").Value);
        Assert.Equal("MT", snapshot.Facts.Single(x => x.Key == "country").Value);
        Assert.Equal("+356 2100 0000", snapshot.Facts.Single(x => x.Key == "phone").Value);
        Assert.Equal("Mo-Fr 07:00-18:00; Sa-Su 08:00-16:00", snapshot.Facts.Single(x => x.Key == "openingHours").Value);
        Assert.DoesNotContain(snapshot.Facts, x => x.Key == "rating" || x.Key == "reviewCount");
        Assert.All(snapshot.Facts, x => Assert.Equal("public-observed", x.EvidenceClass));
        Assert.All(snapshot.Facts, x => Assert.False(x.OwnerConfirmed));
        Assert.All(snapshot.Facts, x => Assert.Equal(observedAt, x.ObservedAt));
    }

    [Fact]
    public void Extractor_UsesOpenGraphWhenStructuredDataIsAbsent()
    {
        const string html = """
            <html><head>
            <meta property="og:title" content="North Shore Plumbing" />
            <meta property="og:description" content="Local plumbing and repair services" />
            </head></html>
            """;

        var snapshot = PublicBusinessExtractor.Extract("website", new Uri("https://northshore.example"), html, DateTimeOffset.UtcNow);

        Assert.Equal("North Shore Plumbing", snapshot.Facts.Single(x => x.Key == "name").Value);
        Assert.Equal("home-local-services", snapshot.Facts.Single(x => x.Key == "category").Value);
        Assert.DoesNotContain(snapshot.Facts, x => x.Key == "phone" || x.Key == "openingHours" || x.Key == "rating");
    }

    [Theory]
    [InlineData("wolt", "restaurant-cafe")]
    [InlineData("bolt-food", "restaurant-cafe")]
    public void Extractor_UsesProviderCategoryBoundary_ForFoodMarketplaces(string provider, string expectedCategory)
    {
        const string html = "<html><head><meta property=\"og:title\" content=\"Test Kitchen\" /></head></html>";
        var snapshot = PublicBusinessExtractor.Extract(provider, new Uri("https://example.com/store"), html, DateTimeOffset.UtcNow);
        Assert.Equal(expectedCategory, snapshot.Facts.Single(x => x.Key == "category").Value);
    }

    [Fact]
    public void Taxonomy_FallsBackToGeneric_WhenNoSupportedCategorySignalExists()
    {
        var result = BusinessCategoryTaxonomy.Infer("Acme Holdings international investments");
        Assert.Equal("generic-business", result.CategoryKey);
        Assert.Null(result.SubcategoryKey);
        Assert.Equal("low", result.Confidence);
    }
}