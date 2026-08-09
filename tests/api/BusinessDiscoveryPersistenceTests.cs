using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryPersistenceTests
{
    [Fact]
    public void Snapshot_IsAccountScoped_AndCanOnlyBeConsumedOnce()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var observedAt = new DateTimeOffset(2026, 8, 9, 19, 45, 0, TimeSpan.Zero);
        var snapshot = BusinessDiscoverySnapshot.Create(ownerId, PublicSnapshot(observedAt));

        Assert.Equal(ownerId, snapshot.UserAccountId);
        Assert.True(snapshot.CanBeConsumedBy(ownerId));
        Assert.False(snapshot.CanBeConsumedBy(otherId));
        Assert.Equal(4, snapshot.Facts.Count);
        Assert.All(snapshot.Facts, fact => Assert.Equal("public-observed", fact.EvidenceClass));
        Assert.All(snapshot.Facts, fact => Assert.False(fact.OwnerConfirmed));

        var businessId = Guid.NewGuid();
        snapshot.MarkConsumed(businessId, observedAt.AddMinutes(2));

        Assert.Equal(businessId, snapshot.BusinessId);
        Assert.NotNull(snapshot.ConsumedAt);
        Assert.False(snapshot.CanBeConsumedBy(ownerId));
        Assert.Throws<InvalidOperationException>(() => snapshot.MarkConsumed(Guid.NewGuid(), observedAt.AddMinutes(3)));
    }

    [Theory]
    [InlineData("Harbour Coffee", "Harbour Coffee", "public", "public-observed", true)]
    [InlineData("Harbour Coffee", "Harbour Coffee Roasters", "owner", "owner-reported", true)]
    [InlineData(null, "Europe/Malta", "owner", "owner-reported", true)]
    public void Provenance_SeparatesAcceptedPublic_OwnerEdits_AndManualValues(
        string? observedValue,
        string authoritativeValue,
        string expectedSource,
        string expectedEvidenceClass,
        bool expectedConfirmed)
    {
        var observedAt = new DateTimeOffset(2026, 8, 9, 19, 45, 0, TimeSpan.Zero);
        var fact = observedValue is null ? null : new BusinessDiscoveryFact
        {
            Id = Guid.NewGuid(), SnapshotId = Guid.NewGuid(), Key = "name", Value = observedValue,
            Source = "website", SourceUrl = "https://harbour.example", ObservedAt = observedAt,
            Confidence = "high", EvidenceClass = "public-observed", OwnerConfirmed = false
        };

        var resolved = BusinessDiscoveryProvenance.Resolve(Guid.NewGuid(), "name", authoritativeValue, fact, observedAt.AddMinutes(1));

        Assert.Equal(expectedSource, resolved.Source);
        Assert.Equal(expectedEvidenceClass, resolved.EvidenceClass);
        Assert.Equal(expectedConfirmed, resolved.OwnerConfirmed);
        if (expectedSource == "public") Assert.Equal("https://harbour.example", resolved.SourceUrl);
        else Assert.Null(resolved.SourceUrl);
    }

    [Fact]
    public async Task Creator_AtomicallyCreatesBusinessProfileProvenanceMembershipPackAndConsumesSnapshot()
    {
        await using var db = CreateDb();
        var owner = Account("owner-a");
        var publicSnapshot = PublicSnapshot(new DateTimeOffset(2026, 8, 9, 19, 45, 0, TimeSpan.Zero));
        var discovery = BusinessDiscoverySnapshot.Create(owner.Id, publicSnapshot);
        var (pack, _) = GenericBusinessKnowledgePack.Create(owner.Id);
        db.Add(owner);
        db.Add(discovery);
        db.KnowledgePacks.Add(pack);
        await db.SaveChangesAsync();

        var request = new CreateBusinessFromDiscoveryRequest(
            discovery.Id,
            "Harbour Coffee",
            "restaurant-cafe",
            "cafe",
            "MT",
            "Europe/Malta",
            "EUR",
            "1 Republic Street, Valletta, MT",
            "Open",
            "Independent coffee shop and bakery",
            "https://harbour.example",
            null,
            null,
            "English",
            true);

        var result = await BusinessDiscoveryBusinessCreator.CreateAsync(db, owner.ProviderSubject, request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Single(await db.BusinessMemberships.Where(x => x.BusinessId == result.Id && x.UserAccountId == owner.Id && x.Role == MembershipRoles.BusinessOwner).ToListAsync());
        Assert.Single(await db.BusinessKnowledgeAssignments.Where(x => x.BusinessId == result.Id && x.PackKey == KnowledgePackKeys.GenericBusiness && x.IsCurrent).ToListAsync());
        var profile = await db.BusinessProfiles.SingleAsync(x => x.BusinessId == result.Id);
        Assert.Equal("Independent coffee shop and bakery", profile.Description);
        Assert.Equal("https://harbour.example", profile.Website);

        var fields = await db.BusinessProfileFields.Where(x => x.BusinessId == result.Id).ToListAsync();
        Assert.Equal("public-observed", fields.Single(x => x.Key == "name").EvidenceClass);
        Assert.Equal("public", fields.Single(x => x.Key == "name").Source);
        Assert.Equal("public-observed", fields.Single(x => x.Key == "category").EvidenceClass);
        Assert.Equal("owner-reported", fields.Single(x => x.Key == "timezone").EvidenceClass);
        Assert.Equal("owner", fields.Single(x => x.Key == "timezone").Source);
        Assert.All(fields, x => Assert.True(x.OwnerConfirmed));

        var consumed = await db.BusinessDiscoverySnapshots.Include(x => x.Facts).SingleAsync(x => x.Id == discovery.Id);
        Assert.Equal(result.Id, consumed.BusinessId);
        Assert.NotNull(consumed.ConsumedAt);
        Assert.Contains(await db.AuditRecords.Where(x => x.BusinessId == result.Id).ToListAsync(), x => x.Action == "business.discovery.confirmed");
    }

    [Fact]
    public async Task Creator_RejectsForeignAndConsumedSnapshots()
    {
        await using var db = CreateDb();
        var owner = Account("owner-a");
        var other = Account("owner-b");
        var discovery = BusinessDiscoverySnapshot.Create(owner.Id, PublicSnapshot(DateTimeOffset.UtcNow));
        db.AddRange(owner, other, discovery);
        await db.SaveChangesAsync();

        var request = ValidRequest(discovery.Id);
        var foreign = await Assert.ThrowsAsync<BusinessDiscoveryException>(() => BusinessDiscoveryBusinessCreator.CreateAsync(db, other.ProviderSubject, request, CancellationToken.None));
        Assert.Equal("business_discovery_not_found", foreign.Code);

        var (pack, _) = GenericBusinessKnowledgePack.Create(owner.Id);
        db.KnowledgePacks.Add(pack);
        await db.SaveChangesAsync();
        await BusinessDiscoveryBusinessCreator.CreateAsync(db, owner.ProviderSubject, request, CancellationToken.None);

        var reused = await Assert.ThrowsAsync<BusinessDiscoveryException>(() => BusinessDiscoveryBusinessCreator.CreateAsync(db, owner.ProviderSubject, request, CancellationToken.None));
        Assert.Equal("business_discovery_consumed", reused.Code);
    }

    private static CreateBusinessFromDiscoveryRequest ValidRequest(Guid snapshotId) => new(
        snapshotId, "Harbour Coffee", "restaurant-cafe", "cafe", "MT", "Europe/Malta", "EUR",
        "1 Republic Street, Valletta, MT", "Open", "Independent coffee shop", null, null, null, "English", true);

    private static PublicBusinessSnapshot PublicSnapshot(DateTimeOffset observedAt) => new(
        "website", "https://harbour.example", observedAt,
        [
            new PublicBusinessFact("name", "Harbour Coffee", "website", "https://harbour.example", observedAt, "high"),
            new PublicBusinessFact("category", "restaurant-cafe", "website", "https://harbour.example", observedAt, "high"),
            new PublicBusinessFact("primaryLocation", "1 Republic Street, Valletta, MT", "website", "https://harbour.example", observedAt, "high"),
            new PublicBusinessFact("description", "Independent coffee shop and bakery", "website", "https://harbour.example", observedAt, "high")
        ]);

    private static AtlasDbContext CreateDb() => new(new DbContextOptionsBuilder<AtlasDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static UserAccount Account(string subject) => new() { Id = Guid.NewGuid(), ProviderSubject = subject, CreatedAt = DateTimeOffset.UtcNow };
}
