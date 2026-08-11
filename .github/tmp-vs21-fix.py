from pathlib import Path

mobile = Path('apps/mobile/app/create-business.tsx')
text = mobile.read_text()
replacements = {
    '<Text numberOfLines={1} style={s.site}>⊕  {providerLabel(discovery.provider)} · {sourceHost(discovery.sourceUrl)}</Text>': '<Text numberOfLines={1} style={s.site}>⊕  Public business page</Text>',
    '<Detail icon="⊕" text={`Observed from ${providerLabel(discovery.provider)}`} />': '<Detail icon="⊕" text="Observed from public business page" />',
    "function providerLabel(provider: string) { return provider === 'bolt-food' ? 'Bolt Food' : provider === 'wolt' ? 'Wolt' : 'Website'; }\n": '',
    "function sourceHost(value: string) { try { return new URL(value).hostname; } catch { return 'public source'; } }\n": '',
}
for old, new in replacements.items():
    if old not in text:
        raise SystemExit(f'missing mobile replacement: {old[:80]}')
    text = text.replace(old, new, 1)
mobile.write_text(text)

api = Path('apps/api/BusinessDiscovery.cs')
text = api.read_text()
old = '''        while (builder.Length <= MaxCharacters)
        {
            var remaining = MaxCharacters + 1 - builder.Length;
            var read = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining)), ct);
            if (read == 0) return builder.ToString();
            builder.Append(buffer, 0, read);
            if (builder.Length > MaxCharacters)
                throw new BusinessDiscoveryException("business_source_too_large", "That business page is too large for safe discovery. Use a smaller public page or set up manually.");
        }

        throw new BusinessDiscoveryException("business_source_too_large", "That business page is too large for safe discovery. Use a smaller public page or set up manually.");'''
new = '''        while (builder.Length < MaxCharacters)
        {
            var remaining = MaxCharacters - builder.Length;
            var read = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining)), ct);
            if (read == 0) break;
            builder.Append(buffer, 0, read);
        }

        return builder.ToString();'''
if old not in text:
    raise SystemExit('missing bounded-reader replacement')
api.write_text(text.replace(old, new, 1))

tests = Path('tests/api/BusinessDiscoveryPolicyTests.cs')
text = tests.read_text()
old = '''    [Fact]
    public async Task HtmlReader_RejectsOversizedResponses()
    {
        using var content = new StringContent(new string('x', PublicBusinessHtmlReader.MaxCharacters + 1));

        var error = await Assert.ThrowsAsync<BusinessDiscoveryException>(() => PublicBusinessHtmlReader.ReadAsync(content, CancellationToken.None));

        Assert.Equal("business_source_too_large", error.Code);
    }'''
new = '''    [Fact]
    public async Task HtmlReader_BoundsLargeMarketplacePages_AndPreservesUsefulPrefix()
    {
        const string metadata = "<html><head><meta property=\\\"og:title\\\" content=\\\"Large Wolt Kitchen\\\" /></head><body>";
        using var content = new StringContent(metadata + new string('x', PublicBusinessHtmlReader.MaxCharacters + 1_000));

        var html = await PublicBusinessHtmlReader.ReadAsync(content, CancellationToken.None);

        Assert.Equal(PublicBusinessHtmlReader.MaxCharacters, html.Length);
        Assert.StartsWith(metadata, html);
        var snapshot = PublicBusinessExtractor.Extract("wolt", new Uri("https://wolt.com/en/mlt/malta/restaurant/large-wolt-kitchen"), html, DateTimeOffset.UtcNow);
        Assert.Equal("Large Wolt Kitchen", snapshot.Facts.Single(x => x.Key == "name").Value);
    }'''
if old not in text:
    raise SystemExit('missing oversized-reader test replacement')
tests.write_text(text.replace(old, new, 1))
