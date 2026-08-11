using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class CategoryIntelligenceHeroJourneyTests
{
    [Fact]
    public async Task Restaurant_cafe_discovery_to_execution_is_evidence_aware_and_owner_isolated()
    {
        var now = new DateTimeOffset(2026, 8, 11, 5, 15, 0, TimeSpan.Zero);
        await using var db = new AtlasDbContext(new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var owner = new UserAccount
        {
            Id = Guid.NewGuid(), ProviderSubject = "vs24-owner", CreatedAt = now.AddMinutes(-5)
        };
        var primaryUrl = "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians";
        var secondaryUrl = "https://antalya.example";
        var primary = new BusinessSourceObservation(
            0, true, "bolt-food", primaryUrl, "success",
            [
                Fact("name", "Antalya Kebab St. Julian's", "bolt-food", primaryUrl, now.AddMinutes(-4)),
                Fact("category", "restaurant-cafe", "bolt-food", primaryUrl, now.AddMinutes(-4)),
                Fact("description", "Turkish kebab restaurant", "bolt-food", primaryUrl, now.AddMinutes(-4))
            ]);
        var secondary = new BusinessSourceObservation(
            1, false, "website", secondaryUrl, "success",
            [
                Fact("name", "Antalya Kebab St. Julian's", "website", secondaryUrl, now.AddMinutes(-3)),
                Fact("category", "restaurant-cafe", "website", secondaryUrl, now.AddMinutes(-3))
            ]);
        var reconciliation = BusinessDiscoveryReconciler.Reconcile([primary, secondary]);
        var snapshot = BusinessDiscoverySnapshot.Create(owner.Id, reconciliation);
        var (corePack, _) = GenericBusinessKnowledgePack.Create(owner.Id);

        db.UserAccounts.Add(owner);
        db.BusinessDiscoverySnapshots.Add(snapshot);
        db.KnowledgePacks.Add(corePack);
        await db.SaveChangesAsync();

        var createRequest = new CreateBusinessFromDiscoveryRequest(
            snapshot.Id,
            "Antalya Kebab St. Julian's",
            "restaurant-cafe",
            "takeaway",
            "MT",
            "Europe/Malta",
            "EUR",
            "St Julian's, Malta",
            "Open",
            "Turkish kebab restaurant",
            secondaryUrl,
            null,
            null,
            "English",
            true);

        var created = await BusinessDiscoveryBusinessCreator.CreateAsync(
            db, owner.ProviderSubject, createRequest, CancellationToken.None);

        var goal = new BusinessGoal
        {
            Id = Guid.NewGuid(),
            BusinessId = created.Id,
            Type = "revenue",
            Title = "Increase revenue",
            Priority = 1,
            IsCustom = false,
            UpdatedAt = now
        };
        var context = new BusinessContextEntry
        {
            Id = Guid.NewGuid(),
            BusinessId = created.Id,
            Key = "primarychannels",
            Value = "Takeaway",
            Source = FieldSources.Owner,
            OwnerConfirmed = true,
            UpdatedAt = now
        };
        db.BusinessGoals.Add(goal);
        db.BusinessContextEntries.Add(context);
        await db.SaveChangesAsync();

        var focus = await OpportunityFocusService.GenerateAsync(
            db, created.Id, owner.Id, now, CancellationToken.None);

        Assert.Equal(OpportunityFocusGenerationStates.Ready, focus.State);
        var opportunity = Assert.IsType<Opportunity>(focus.Opportunity);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.PackKey, opportunity.KnowledgePackKey);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.Version, opportunity.KnowledgePackVersion);
        Assert.Contains("ordering", opportunity.Title, StringComparison.OrdinalIgnoreCase);

        var detail = OpportunityPolicy.Detail(opportunity, goal, now);
        Assert.NotEmpty(detail.Evidence);
        Assert.Contains(detail.Evidence, item => item.Label == "Primary channels" && item.Value == "Takeaway");
        Assert.Equal("Increase revenue", detail.GoalTitle);
        Assert.True(detail.ExecutionKitAvailable);

        var kit = ExecutionKitFactory.Create(opportunity, now);
        var expectedTemplate = RestaurantCafeKnowledgeManifestV2.Create().ExecutionTemplates
            .Single(x => x.Key == "ordering-path-review-checklist");
        Assert.Contains(kit.Assets, asset =>
            asset.Type == expectedTemplate.AssetType &&
            asset.Title == expectedTemplate.Title &&
            asset.Content == expectedTemplate.ContentTemplate);

        var persistedSnapshot = await db.BusinessDiscoverySnapshots
            .Include(x => x.Sources)
            .Include(x => x.Evidence)
            .SingleAsync(x => x.Id == snapshot.Id);
        Assert.Equal(2, persistedSnapshot.Sources.Count);
        Assert.NotEmpty(persistedSnapshot.Evidence);
        Assert.Equal(created.Id, persistedSnapshot.BusinessId);
        Assert.NotNull(persistedSnapshot.ConsumedAt);

        var wrongOwner = await OpportunityFocusService.GenerateAsync(
            db, created.Id, Guid.NewGuid(), now, CancellationToken.None);
        Assert.Equal(OpportunityFocusGenerationStates.Degraded, wrongOwner.State);
        Assert.Equal("business_access_unavailable", wrongOwner.Code);
        Assert.Null(wrongOwner.Opportunity);
    }

    private static PublicBusinessFact Fact(
        string key,
        string value,
        string provider,
        string sourceUrl,
        DateTimeOffset observedAt) =>
        new(key, value, provider, sourceUrl, observedAt, "high");
}
