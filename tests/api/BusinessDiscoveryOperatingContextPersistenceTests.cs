using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryOperatingContextPersistenceTests
{
    [Fact]
    public async Task Confirmation_materializes_operating_context_as_owner_confirmed_without_caching_google_content()
    {
        await using var db = CreateDb();
        var owner = Account("owner-a");
        var observedAt = new DateTimeOffset(2026, 8, 11, 15, 0, 0, TimeSpan.Zero);
        var discovery = BusinessDiscoverySnapshot.Create(owner.Id, new PublicBusinessSnapshot(
            "website",
            "https://harbour.example",
            observedAt,
            [
                new PublicBusinessFact("name", "Harbour Coffee", "website", "https://harbour.example", observedAt, "high"),
                new PublicBusinessFact("category", "restaurant-cafe", "website", "https://harbour.example", observedAt, "high"),
                new PublicBusinessFact("primaryLocation", "1 Republic Street, Valletta, MT", "website", "https://harbour.example", observedAt, "high")
            ]));
        var (pack, _) = GenericBusinessKnowledgePack.Create(owner.Id);
        db.Add(owner);
        db.Add(discovery);
        db.KnowledgePacks.Add(pack);
        await db.SaveChangesAsync();

        var factCount = discovery.Facts.Count;
        var evidenceCount = discovery.Evidence.Count;
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
            "Independent coffee shop",
            "https://harbour.example",
            null,
            null,
            "English",
            true,
            new ConfirmedOperatingContext(
                "ChIJAtlas123",
                ["Dine in", "Takeaway", "Delivery"],
                true,
                ["Lunch", "Dinner"],
                "Moderate"));

        var result = await BusinessDiscoveryBusinessCreator.CreateAsync(db, owner.ProviderSubject, request, CancellationToken.None);

        var context = await db.BusinessContextEntries
            .Where(x => x.BusinessId == result.Id)
            .OrderBy(x => x.Key)
            .ToListAsync();
        Assert.Equal(4, context.Count);
        Assert.All(context, item =>
        {
            Assert.Equal(FieldSources.Owner, item.Source);
            Assert.True(item.OwnerConfirmed);
        });
        Assert.Equal("Dine in | Takeaway | Delivery", context.Single(x => x.Key == "operatingchannels").Value);
        Assert.Equal("Available", context.Single(x => x.Key == "reservationcapability").Value);
        Assert.Equal("Lunch | Dinner", context.Single(x => x.Key == "serviceperiods").Value);
        Assert.Equal("Moderate", context.Single(x => x.Key == "priceposition").Value);
        Assert.DoesNotContain(context, item => item.Value.Contains("ChIJAtlas123", StringComparison.Ordinal));

        var consumed = await db.BusinessDiscoverySnapshots
            .Include(x => x.Facts)
            .Include(x => x.Evidence)
            .SingleAsync(x => x.Id == discovery.Id);
        Assert.Equal(factCount, consumed.Facts.Count);
        Assert.Equal(evidenceCount, consumed.Evidence.Count);
        Assert.DoesNotContain(consumed.Facts, item => item.Value.Contains("ChIJAtlas123", StringComparison.Ordinal));
        Assert.DoesNotContain(consumed.Evidence, item => item.Value.Contains("ChIJAtlas123", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Unknown channel", null, null)]
    [InlineData(null, "Late night", null)]
    [InlineData(null, null, "Luxury-plus")]
    public void Confirmation_rejects_operating_context_outside_canonical_allowlists(
        string? invalidChannel,
        string? invalidServicePeriod,
        string? invalidPrice)
    {
        var context = new ConfirmedOperatingContext(
            "ChIJAtlas123",
            invalidChannel is null ? ["Dine in"] : [invalidChannel],
            null,
            invalidServicePeriod is null ? ["Lunch"] : [invalidServicePeriod],
            invalidPrice ?? "Moderate");
        var request = ValidRequest(Guid.NewGuid(), context);

        var errors = request.Validate();

        Assert.Contains(nameof(request.ConfirmedOperatingContext), errors.Keys);
    }

    private static CreateBusinessFromDiscoveryRequest ValidRequest(Guid snapshotId, ConfirmedOperatingContext context) => new(
        snapshotId,
        "Harbour Coffee",
        "restaurant-cafe",
        "cafe",
        "MT",
        "Europe/Malta",
        "EUR",
        "1 Republic Street, Valletta, MT",
        "Open",
        "Independent coffee shop",
        null,
        null,
        null,
        "English",
        true,
        context);

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
}
