using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessPlaceEnrichmentEndpointTests
{
    [Fact]
    public async Task Enrichment_requires_discovery_snapshot_ownership()
    {
        await using var db = CreateDb();
        var owner = Account("owner-a");
        var other = Account("owner-b");
        var snapshot = Snapshot(owner.Id);
        db.UserAccounts.AddRange(owner, other);
        db.BusinessDiscoverySnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessPlaceEnrichmentException>(() =>
            BusinessPlaceEnrichmentService.GetAsync(
                db, other.ProviderSubject, snapshot.Id, "place-atlas", new FixedProvider(Enrichment()), CancellationToken.None));

        Assert.Equal("business_place_enrichment_not_found", exception.Code);
    }

    [Fact]
    public async Task Enrichment_returns_provider_neutral_operating_shape_for_owned_snapshot()
    {
        await using var db = CreateDb();
        var owner = Account("owner-a");
        var snapshot = Snapshot(owner.Id);
        db.UserAccounts.Add(owner);
        db.BusinessDiscoverySnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var result = await BusinessPlaceEnrichmentService.GetAsync(
            db, owner.ProviderSubject, snapshot.Id, "place-atlas", new FixedProvider(Enrichment()), CancellationToken.None);

        Assert.Equal("place-atlas", result.ProviderRef);
        Assert.Equal(["Dine in", "Takeaway"], result.OperatingChannels);
        Assert.True(result.Reservable);
        Assert.Equal(["Lunch", "Dinner"], result.ServicePeriods);
        Assert.Equal("Moderate", result.PricePosition);
        Assert.Equal("Google Maps", result.AttributionLabel);
    }

    [Fact]
    public async Task Enrichment_unavailable_has_stable_degraded_error_code()
    {
        await using var db = CreateDb();
        var owner = Account("owner-a");
        var snapshot = Snapshot(owner.Id);
        db.UserAccounts.Add(owner);
        db.BusinessDiscoverySnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessPlaceEnrichmentException>(() =>
            BusinessPlaceEnrichmentService.GetAsync(
                db, owner.ProviderSubject, snapshot.Id, "place-atlas", new FixedProvider(null), CancellationToken.None));

        Assert.Equal("business_place_enrichment_unavailable", exception.Code);
    }

    [Fact]
    public async Task Enrichment_never_persists_transient_google_operating_content()
    {
        await using var db = CreateDb();
        var owner = Account("owner-a");
        var snapshot = Snapshot(owner.Id);
        db.UserAccounts.Add(owner);
        db.BusinessDiscoverySnapshots.Add(snapshot);
        await db.SaveChangesAsync();
        var factsBefore = await db.Set<BusinessDiscoveryFact>().CountAsync();
        var evidenceBefore = await db.Set<BusinessDiscoveryEvidence>().CountAsync();
        var contextBefore = await db.BusinessContextEntries.CountAsync();

        _ = await BusinessPlaceEnrichmentService.GetAsync(
            db, owner.ProviderSubject, snapshot.Id, "place-atlas", new FixedProvider(Enrichment()), CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal(factsBefore, await db.Set<BusinessDiscoveryFact>().CountAsync());
        Assert.Equal(evidenceBefore, await db.Set<BusinessDiscoveryEvidence>().CountAsync());
        Assert.Equal(contextBefore, await db.BusinessContextEntries.CountAsync());
    }

    private static BusinessPlaceEnrichment Enrichment() => new(
        "place-atlas",
        new DateTimeOffset(2026, 8, 11, 15, 0, 0, TimeSpan.Zero),
        ["Dine in", "Takeaway"],
        true,
        ["Lunch", "Dinner"],
        "Moderate",
        ["Monday: 11:00 AM – 10:00 PM"],
        [new BusinessPlaceAttribution("Example provider", "https://example.com/provider")]);

    private static BusinessDiscoverySnapshot Snapshot(Guid accountId)
    {
        var observedAt = new DateTimeOffset(2026, 8, 11, 15, 0, 0, TimeSpan.Zero);
        return BusinessDiscoverySnapshot.Create(accountId, new PublicBusinessSnapshot(
            "website",
            "https://atlas.example/business",
            observedAt,
            [new PublicBusinessFact(
                "name", "Atlas Test Business", "website", "https://atlas.example/business",
                observedAt, "high", "public-observed", false)]));
    }

    private static AtlasDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static UserAccount Account(string subject) => new()
    {
        Id = Guid.NewGuid(),
        ProviderSubject = subject,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private sealed class FixedProvider(BusinessPlaceEnrichment? value) : IBusinessPlaceEnrichmentProvider
    {
        public bool IsConfigured => true;
        public Task<BusinessPlaceEnrichment?> GetAsync(string providerRef, CancellationToken ct) => Task.FromResult(value);
    }
}
