using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OpportunityReadinessRegressionTests
{
    [Fact]
    public async Task Confirmed_profile_and_active_pack_without_goal_identifies_goal_as_missing()
    {
        var now = new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase($"readiness-{Guid.NewGuid():N}")
            .Options;
        await using var db = new AtlasDbContext(options);
        var core = GenericBusinessKnowledgeManifestV2.Create();

        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            ProviderSubject = "readiness-owner",
            CreatedAt = now
        };
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Atlas Test Restaurant",
            Category = "restaurant-cafe",
            Country = "MT",
            Timezone = "Europe/Malta",
            Currency = "EUR",
            PrimaryLocation = "Birkirkara, Malta",
            OperatingStatus = "Open",
            CreatedAt = now
        };
        var profile = new BusinessProfile
        {
            BusinessId = business.Id,
            Language = "en",
            Source = FieldSources.Owner,
            OwnerConfirmed = true,
            UpdatedAt = now
        };
        var assignment = new BusinessKnowledgeAssignment
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            KnowledgePackId = Guid.NewGuid(),
            KnowledgePackVersionId = Guid.NewGuid(),
            PackKey = core.PackKey,
            ExactVersion = core.ExactVersion,
            IsCurrent = true,
            AssignedByUserAccountId = account.Id,
            AssignedAt = now,
            EffectiveAt = now
        };
        var membership = new BusinessMembership
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            UserAccountId = account.Id,
            UserAccount = account,
            Role = MembershipRoles.BusinessOwner,
            CreatedAt = now
        };

        db.AddRange(account, business, profile, assignment, membership);
        await db.SaveChangesAsync();

        var result = await OpportunityFocusService.GenerateAsync(
            db,
            business.Id,
            account.Id,
            now,
            CancellationToken.None);

        Assert.Equal(OpportunityFocusGenerationStates.InsufficientContext, result.State);
        Assert.Equal("opportunity_goal_missing", result.Code);
        Assert.Null(result.Opportunity);
    }
}
