using System.Text.Json;
using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OpportunityGenerationIntegrationTests
{
    [Fact]
    public async Task Restaurant_bundle_persists_one_category_specific_today_focus()
    {
        await using var db = CreateDb();
        var setup = SeedBusiness(db, category: "restaurant-cafe", goalType: "revenue", contextKey: "primarychannels", contextValue: "Takeaway");
        await db.SaveChangesAsync();

        var result = await OpportunityFocusService.GenerateAsync(db, setup.Business.Id, setup.Account.Id, Now, CancellationToken.None);

        Assert.Equal(OpportunityFocusGenerationStates.Ready, result.State);
        var opportunity = Assert.IsType<Opportunity>(result.Opportunity);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.PackKey, opportunity.KnowledgePackKey);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.Version, opportunity.KnowledgePackVersion);
        Assert.Equal(setup.Assignment.KnowledgePackVersionId, opportunity.KnowledgePackVersionId);
        Assert.Equal(setup.Goal.Id, opportunity.GoalId);
        Assert.Single(await db.Set<Opportunity>().ToListAsync());

        using var snapshot = JsonDocument.Parse(opportunity.EvidenceJson);
        Assert.Equal("ordering-path-clarity-review", snapshot.RootElement.GetProperty("patternKey").GetString());
        Assert.False(string.IsNullOrWhiteSpace(snapshot.RootElement.GetProperty("bundleFingerprint").GetString()));
        Assert.Contains(snapshot.RootElement.GetProperty("manifests").EnumerateArray(),
            x => x.GetProperty("packKey").GetString() == RestaurantCafeKnowledgeManifestV2.PackKey);
    }

    [Fact]
    public async Task Valid_setup_with_no_qualifying_pattern_returns_no_focus_and_persists_nothing()
    {
        await using var db = CreateDb();
        var setup = SeedBusiness(db, category: "restaurant-cafe", goalType: "profitability");
        await db.SaveChangesAsync();

        var result = await OpportunityFocusService.GenerateAsync(db, setup.Business.Id, setup.Account.Id, Now, CancellationToken.None);

        Assert.Equal(OpportunityFocusGenerationStates.NoFocus, result.State);
        Assert.Null(result.Opportunity);
        Assert.Empty(await db.Set<Opportunity>().ToListAsync());
    }

    [Fact]
    public async Task Existing_unexpired_today_focus_is_reused_without_creating_duplicate()
    {
        await using var db = CreateDb();
        var setup = SeedBusiness(db, category: "restaurant-cafe", goalType: "revenue", contextKey: "primarychannels", contextValue: "Takeaway");
        var existing = ExistingOpportunity(setup, Now.AddHours(6));
        db.Set<Opportunity>().Add(existing);
        await db.SaveChangesAsync();

        var result = await OpportunityFocusService.GenerateAsync(db, setup.Business.Id, setup.Account.Id, Now, CancellationToken.None);

        Assert.Equal(OpportunityFocusGenerationStates.Ready, result.State);
        Assert.Equal(existing.Id, result.Opportunity?.Id);
        Assert.Single(await db.Set<Opportunity>().ToListAsync());
    }

    [Fact]
    public async Task Missing_confirmed_profile_or_goal_returns_insufficient_context_without_persisting()
    {
        await using var db = CreateDb();
        var setup = SeedBusiness(db, category: "restaurant-cafe", goalType: "revenue", contextKey: "primarychannels", contextValue: "Takeaway");
        setup.Profile.OwnerConfirmed = false;
        await db.SaveChangesAsync();

        var result = await OpportunityFocusService.GenerateAsync(db, setup.Business.Id, setup.Account.Id, Now, CancellationToken.None);

        Assert.Equal(OpportunityFocusGenerationStates.InsufficientContext, result.State);
        Assert.Null(result.Opportunity);
        Assert.Empty(await db.Set<Opportunity>().ToListAsync());
    }

    [Fact]
    public async Task Different_business_owner_cannot_generate_or_read_target_business_focus_through_service()
    {
        await using var db = CreateDb();
        var target = SeedBusiness(db, category: "restaurant-cafe", goalType: "revenue", contextKey: "primarychannels", contextValue: "Takeaway");
        var other = SeedBusiness(db, category: "retail", goalType: "revenue");
        await db.SaveChangesAsync();

        var result = await OpportunityFocusService.GenerateAsync(db, target.Business.Id, other.Account.Id, Now, CancellationToken.None);

        Assert.Equal(OpportunityFocusGenerationStates.Degraded, result.State);
        Assert.Equal("business_access_unavailable", result.Code);
        Assert.Null(result.Opportunity);
        Assert.Empty(await db.Set<Opportunity>().ToListAsync());
    }

    [Fact]
    public async Task Bundle_resolution_failure_degrades_without_server_side_generation_or_row()
    {
        await using var db = CreateDb();
        var setup = SeedBusiness(db, category: "restaurant-cafe", goalType: "revenue", contextKey: "primarychannels", contextValue: "Takeaway");
        setup.Assignment.ExactVersion = "999.0";
        await db.SaveChangesAsync();

        var result = await OpportunityFocusService.GenerateAsync(db, setup.Business.Id, setup.Account.Id, Now, CancellationToken.None);

        Assert.Equal(OpportunityFocusGenerationStates.Degraded, result.State);
        Assert.Equal("core_manifest_unavailable", result.Code);
        Assert.Null(result.Opportunity);
        Assert.Empty(await db.Set<Opportunity>().ToListAsync());
    }

    [Fact]
    public async Task Expired_focus_is_marked_expired_before_a_new_eligible_focus_is_created()
    {
        await using var db = CreateDb();
        var setup = SeedBusiness(db, category: "restaurant-cafe", goalType: "revenue", contextKey: "primarychannels", contextValue: "Takeaway");
        var expired = ExistingOpportunity(setup, Now.AddMinutes(-1));
        db.Set<Opportunity>().Add(expired);
        await db.SaveChangesAsync();

        var result = await OpportunityFocusService.GenerateAsync(db, setup.Business.Id, setup.Account.Id, Now, CancellationToken.None);

        Assert.Equal(OpportunityFocusGenerationStates.Ready, result.State);
        Assert.NotEqual(expired.Id, result.Opportunity?.Id);
        Assert.Equal(OpportunityStatuses.Expired, expired.Status);
        Assert.Equal(2, await db.Set<Opportunity>().CountAsync());
    }

    private static readonly DateTimeOffset Now = new(2026, 8, 11, 2, 0, 0, TimeSpan.Zero);

    private static AtlasDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase($"vs23-{Guid.NewGuid():N}")
            .Options;
        return new AtlasDbContext(options);
    }

    private static SeededBusiness SeedBusiness(
        AtlasDbContext db,
        string category,
        string goalType,
        string? contextKey = null,
        string? contextValue = null)
    {
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var account = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = $"subject-{Guid.NewGuid():N}", CreatedAt = Now };
        var business = new Business
        {
            Id = Guid.NewGuid(), Name = "Atlas Test Restaurant", Category = category, Country = "MT", Timezone = "Europe/Malta",
            Currency = "EUR", PrimaryLocation = "St Julian's, Malta", OperatingStatus = "Open", CreatedAt = Now
        };
        var profile = new BusinessProfile
        {
            BusinessId = business.Id, Language = "en", Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = Now
        };
        var goal = new BusinessGoal
        {
            Id = Guid.NewGuid(), BusinessId = business.Id, Type = goalType, Title = "Owner priority", Priority = 1, UpdatedAt = Now
        };
        var assignment = new BusinessKnowledgeAssignment
        {
            Id = Guid.NewGuid(), BusinessId = business.Id, KnowledgePackId = Guid.NewGuid(), KnowledgePackVersionId = Guid.NewGuid(),
            PackKey = core.PackKey, ExactVersion = core.ExactVersion,
            IsCurrent = true, AssignedByUserAccountId = account.Id, AssignedAt = Now, EffectiveAt = Now
        };
        var membership = new BusinessMembership
        {
            Id = Guid.NewGuid(), BusinessId = business.Id, UserAccountId = account.Id, UserAccount = account,
            Role = MembershipRoles.BusinessOwner, CreatedAt = Now
        };

        db.UserAccounts.Add(account);
        db.Businesses.Add(business);
        db.BusinessProfiles.Add(profile);
        db.BusinessGoals.Add(goal);
        db.BusinessKnowledgeAssignments.Add(assignment);
        db.BusinessMemberships.Add(membership);

        if (!string.IsNullOrWhiteSpace(contextKey) && !string.IsNullOrWhiteSpace(contextValue))
        {
            db.BusinessContextEntries.Add(new BusinessContextEntry
            {
                Id = Guid.NewGuid(), BusinessId = business.Id, Key = contextKey, Value = contextValue,
                Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = Now
            });
        }

        return new SeededBusiness(account, business, profile, goal, assignment);
    }

    private static Opportunity ExistingOpportunity(SeededBusiness setup, DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(), BusinessId = setup.Business.Id, GoalId = setup.Goal.Id,
        Title = "Existing focus", WhyItMatters = "Existing", WhyNow = "Existing", ExpectedImpact = "Existing",
        Effort = "Low", Confidence = "Medium", EvidenceSummary = "Existing", EvidenceJson = "{}",
        Status = OpportunityStatuses.Available, KnowledgePackKey = setup.Assignment.PackKey,
        KnowledgePackVersion = setup.Assignment.ExactVersion, KnowledgePackVersionId = setup.Assignment.KnowledgePackVersionId,
        CreatedAt = Now.AddHours(-1), ExpiresAt = expiresAt
    };

    private sealed record SeededBusiness(
        UserAccount Account,
        Business Business,
        BusinessProfile Profile,
        BusinessGoal Goal,
        BusinessKnowledgeAssignment Assignment);
}
