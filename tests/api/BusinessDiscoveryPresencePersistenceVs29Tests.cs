using System.Text.Json;
using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryPresencePersistenceVs29Tests
{
    [Fact]
    public async Task Creator_materializes_confirmed_email_social_and_hours_with_truthful_provenance()
    {
        await using var db = CreateDb();
        var owner = Account("owner-vs29");
        var observedAt = new DateTimeOffset(2026, 8, 12, 5, 0, 0, TimeSpan.Zero);
        var discovery = BusinessDiscoverySnapshot.Create(owner.Id, new PublicBusinessSnapshot(
            "website",
            "https://harbour.example",
            observedAt,
            [
                new PublicBusinessFact("name", "Harbour Coffee", "website", "https://harbour.example", observedAt, "high"),
                new PublicBusinessFact("category", "restaurant-cafe", "website", "https://harbour.example", observedAt, "high"),
                new PublicBusinessFact("primaryLocation", "1 Republic Street, Valletta, MT", "website", "https://harbour.example", observedAt, "high"),
                new PublicBusinessFact("email", "hello@harbour.example", "website", "https://harbour.example", observedAt, "high"),
                new PublicBusinessFact("socialChannels", "https://instagram.com/harbourcoffee/", "website", "https://harbour.example", observedAt, "high")
            ]));
        var (pack, _) = GenericBusinessKnowledgePack.Create(owner.Id);
        db.Add(owner);
        db.Add(discovery);
        db.KnowledgePacks.Add(pack);
        await db.SaveChangesAsync();

        var request = JsonSerializer.Deserialize<CreateBusinessFromDiscoveryRequest>($$"""
        {
          "snapshotId":"{{discovery.Id}}",
          "name":"Harbour Coffee",
          "category":"restaurant-cafe",
          "subcategory":"cafe",
          "country":"MT",
          "timezone":"Europe/Malta",
          "currency":"EUR",
          "primaryLocation":"1 Republic Street, Valletta, MT",
          "operatingStatus":"Open",
          "description":"Independent coffee shop",
          "website":"https://harbour.example",
          "phone":null,
          "businessHours":null,
          "language":"English",
          "ownerConfirmed":true,
          "confirmedOperatingContext":{
            "providerRef":"ChIJAtlasVs29",
            "operatingChannels":["Takeaway"],
            "reservable":true,
            "servicePeriods":["Lunch"],
            "pricePosition":"Moderate",
            "openingHours":[
              "Monday: 08:00-18:00",
              "Tuesday: 08:00-18:00"
            ]
          },
          "email":"hello@harbour.example",
          "socialChannels":"https://instagram.com/harbourcoffee/"
        }
        """, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(request);

        var result = await BusinessDiscoveryBusinessCreator.CreateAsync(db, owner.ProviderSubject, request!, CancellationToken.None);

        var profile = await db.BusinessProfiles.SingleAsync(x => x.BusinessId == result.Id);
        Assert.Equal("hello@harbour.example", profile.Email);
        Assert.Equal("https://instagram.com/harbourcoffee/", profile.SocialChannels);
        Assert.Equal("Monday: 08:00-18:00\nTuesday: 08:00-18:00", profile.BusinessHours);

        var fields = await db.BusinessProfileFields.Where(x => x.BusinessId == result.Id).ToListAsync();
        var email = fields.Single(x => x.Key == "email");
        Assert.Equal(FieldSources.Public, email.Source);
        Assert.Equal("public-observed", email.EvidenceClass);
        Assert.True(email.OwnerConfirmed);
        var social = fields.Single(x => x.Key == "socialChannels");
        Assert.Equal(FieldSources.Public, social.Source);
        Assert.True(social.OwnerConfirmed);
        var hours = fields.Single(x => x.Key == "openingHours");
        Assert.Equal(FieldSources.Owner, hours.Source);
        Assert.Equal("owner-reported", hours.EvidenceClass);
        Assert.True(hours.OwnerConfirmed);
    }

    [Fact]
    public void More_than_seven_confirmed_hours_are_rejected()
    {
        var context = JsonSerializer.Deserialize<ConfirmedOperatingContext>("""
        {
          "providerRef":"ChIJAtlasVs29",
          "operatingChannels":[],
          "reservable":null,
          "servicePeriods":[],
          "pricePosition":null,
          "openingHours":["1","2","3","4","5","6","7","8"]
        }
        """, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(context);
        Assert.False(context!.IsValid());
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
}
