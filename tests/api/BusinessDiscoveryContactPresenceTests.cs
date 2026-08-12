using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryContactPresenceTests
{
    [Fact]
    public void Extract_reads_structured_email_and_allowlisted_social_channels()
    {
        const string html = """
            <script type="application/ld+json">
            {
              "@context":"https://schema.org",
              "@type":"Restaurant",
              "name":"Atlas Test Cafe",
              "url":"https://example.com",
              "telephone":"+356 2100 0000",
              "email":"hello@example.com",
              "sameAs":[
                "https://www.instagram.com/atlas-test-cafe/#menu",
                "https://FACEBOOK.com/atlas-test-cafe/",
                "https://example.net/not-a-social-profile"
              ]
            }
            </script>
            """;
        var observedAt = new DateTimeOffset(2026, 8, 12, 4, 0, 0, TimeSpan.Zero);

        var snapshot = PublicBusinessExtractor.Extract("website", new Uri("https://example.com"), html, observedAt);

        var email = snapshot.Facts.Single(x => x.Key == "email");
        Assert.Equal("hello@example.com", email.Value);
        Assert.Equal("website", email.Source);
        Assert.Equal("https://example.com/", email.SourceUrl);
        Assert.Equal(observedAt, email.ObservedAt);
        Assert.Equal("high", email.Confidence);

        var social = snapshot.Facts.Single(x => x.Key == "socialChannels");
        Assert.Contains("instagram.com/atlas-test-cafe", social.Value, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("facebook.com/atlas-test-cafe", social.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("example.net", social.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain('#', social.Value);
    }

    [Fact]
    public void Extract_uses_one_unambiguous_mailto_and_tel_fallback()
    {
        const string html = """
            <html><head><meta property="og:title" content="Atlas Test Cafe" /></head>
            <body>
              <a href="mailto:Hello@Example.com">Email us</a>
              <a href="mailto:hello@example.com">Contact</a>
              <a href="tel:+35621000000">Call</a>
              <a href="tel:+35621000000">Phone</a>
            </body></html>
            """;

        var snapshot = PublicBusinessExtractor.Extract("website", new Uri("https://example.com"), html, DateTimeOffset.UtcNow);

        Assert.Equal("hello@example.com", snapshot.Facts.Single(x => x.Key == "email").Value, ignoreCase: true);
        Assert.Equal("+35621000000", snapshot.Facts.Single(x => x.Key == "phone").Value);
    }

    [Fact]
    public void Extract_omits_ambiguous_mailto_and_tel_fallbacks()
    {
        const string html = """
            <html><head><meta property="og:title" content="Atlas Test Cafe" /></head>
            <body>
              <a href="mailto:first@example.com">Email one</a>
              <a href="mailto:second@example.com">Email two</a>
              <a href="tel:+35621000000">Phone one</a>
              <a href="tel:+35622000000">Phone two</a>
            </body></html>
            """;

        var snapshot = PublicBusinessExtractor.Extract("website", new Uri("https://example.com"), html, DateTimeOffset.UtcNow);

        Assert.DoesNotContain(snapshot.Facts, x => x.Key == "email");
        Assert.DoesNotContain(snapshot.Facts, x => x.Key == "phone");
    }
}
