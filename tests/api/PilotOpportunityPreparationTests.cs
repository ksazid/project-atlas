using System.Reflection;
using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class PilotOpportunityPreparationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 16, 0, 0, TimeSpan.Zero);
    private static Assembly ApiAssembly => typeof(AtlasDbContext).Assembly;

    [Fact]
    public async Task Preview_returns_one_current_factual_evidence_candidate_without_free_form_authoring()
    {
        await using var db = CreateDb();
        var setup = SeedBusiness(db, "restaurant-cafe", "revenue", "operatingchannels", "Dine in | Takeaway | Delivery");
        await db.SaveChangesAsync();

        var candidate = await InvokeAsync("PreviewOpportunityAsync", db, setup.Business.Id, Now, CancellationToken.None);

        Assert.NotNull(candidate);
        Assert.Equal("ordering-path-clarity-review", Property(candidate!, "PatternKey"));
        Assert.False(string.IsNullOrWhiteSpace(Assert.IsType<string>(Property(candidate!, "BundleFingerprint"))));
        Assert.True(Assert.IsType<int>(Property(candidate!, "EvidenceCount")) > 0);
        var requestType = RequireType("PilotPrepareOpportunityRequest");
        Assert.NotNull(requestType.GetProperty("PatternKey"));
        Assert.NotNull(requestType.GetProperty("BundleFingerprint"));
        Assert.NotNull(requestType.GetProperty("Reason"));
        Assert.Null(requestType.GetProperty("Title"));
        Assert.Null(requestType.GetProperty("WhyItMatters"));
    }

    [Fact]
    public async Task Preview_requires_confirmed_profile_goal_current_pack_and_factual_evidence()
    {
        await using var db = CreateDb();
        var setup = SeedBusiness(db, "restaurant-cafe", "revenue");
        await db.SaveChangesAsync();
        Assert.Null(await InvokeAsync("PreviewOpportunityAsync", db, setup.Business.Id, Now, CancellationToken.None));

        db.BusinessContextEntries.Add(new BusinessContextEntry
        {
            Id = Guid.NewGuid(), BusinessId = setup.Business.Id, Key = "operatingchannels", Value = "Takeaway",
            Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = Now
        });
        setup.Profile.OwnerConfirmed = false;
        await db.SaveChangesAsync();
        Assert.Null(await InvokeAsync("PreviewOpportunityAsync", db, setup.Business.Id, Now, CancellationToken.None));
    }

    [Fact]
    public async Task Prepare_persists_only_the_recomputed_matching_candidate_with_operator_provenance()
    {
        await using var db = CreateDb();
        var setup = SeedBusiness(db, "restaurant-cafe", "revenue", "operatingchannels", "Takeaway");
        var operatorAccount = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "operator", CreatedAt = Now };
        db.UserAccounts.Add(operatorAccount);
        await db.SaveChangesAsync();

        var candidate = await InvokeAsync("PreviewOpportunityAsync", db, setup.Business.Id, Now, CancellationToken.None);
        Assert.NotNull(candidate);
        var request = NewPrepareRequest(candidate!, "Owner asked us to prepare the evidence-backed option.");
        var result = await InvokeAsync("PrepareOpportunityAsync", db, setup.Business.Id, operatorAccount, request, Now, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("prepared", Property(result!, "State"));
        var opportunity = await db.Opportunities.SingleAsync();
        Assert.Equal(setup.Business.Id, opportunity.BusinessId);
        Assert.Equal(Property(candidate!, "PatternKey"), ReadPatternKey(opportunity.EvidenceJson));
        var operation = await db.PilotOperationRecords.SingleAsync();
        Assert.Equal(PilotOperationActions.OpportunityPrepared, operation.Action);
        Assert.Equal(opportunity.Id, operation.TargetId);
        Assert.Contains("bundleFingerprint", operation.MetadataJson ?? string.Empty, StringComparison.Ordinal);
        Assert.Single(await db.AuditRecords.Where(x => x.BusinessId == setup.Business.Id).ToListAsync());
    }

    [Fact]
    public async Task Prepare_rejects_stale_candidate_existing_active_opportunity_and_cross_business_candidate()
    {
        await using var db = CreateDb();
        var first = SeedBusiness(db, "restaurant-cafe", "revenue", "operatingchannels", "Takeaway");
        var second = SeedBusiness(db, "restaurant-cafe", "revenue", "operatingchannels", "Delivery");
        var operatorAccount = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "operator", CreatedAt = Now };
        db.UserAccounts.Add(operatorAccount);
        await db.SaveChangesAsync();

        var firstCandidate = await InvokeAsync("PreviewOpportunityAsync", db, first.Business.Id, Now, CancellationToken.None);
        Assert.NotNull(firstCandidate);
        var staleRequest = NewPrepareRequest(firstCandidate!, "Prepare after review.");
        var firstContext = await db.BusinessContextEntries.SingleAsync(x => x.BusinessId == first.Business.Id && x.Key == "operatingchannels");
        firstContext.Value = "Dine in";
        firstContext.UpdatedAt = Now.AddMinutes(1);
        await db.SaveChangesAsync();
        var stale = await InvokeAsync("PrepareOpportunityAsync", db, first.Business.Id, operatorAccount, staleRequest, Now.AddMinutes(1), CancellationToken.None);
        Assert.NotNull(stale);
        Assert.Equal("stale", Property(stale!, "State"));
        Assert.Empty(await db.Opportunities.Where(x => x.BusinessId == first.Business.Id).ToListAsync());

        var cross = await InvokeAsync("PrepareOpportunityAsync", db, second.Business.Id, operatorAccount, staleRequest, Now, CancellationToken.None);
        Assert.NotNull(cross);
        Assert.Equal("stale", Property(cross!, "State"));
        Assert.Empty(await db.Opportunities.Where(x => x.BusinessId == second.Business.Id).ToListAsync());

        var secondCandidate = await InvokeAsync("PreviewOpportunityAsync", db, second.Business.Id, Now, CancellationToken.None);
        Assert.NotNull(secondCandidate);
        db.Opportunities.Add(ExistingOpportunity(second, Now.AddHours(2)));
        await db.SaveChangesAsync();
        var conflict = await InvokeAsync("PrepareOpportunityAsync", db, second.Business.Id, operatorAccount, NewPrepareRequest(secondCandidate!, "Prepare."), Now, CancellationToken.None);
        Assert.NotNull(conflict);
        Assert.Equal("conflict", Property(conflict!, "State"));
        Assert.Single(await db.Opportunities.Where(x => x.BusinessId == second.Business.Id).ToListAsync());
    }

    private static object NewPrepareRequest(object candidate, string reason)
    {
        var type = RequireType("PilotPrepareOpportunityRequest");
        var value = Activator.CreateInstance(type, [
            Assert.IsType<string>(Property(candidate, "PatternKey")),
            Assert.IsType<string>(Property(candidate, "BundleFingerprint")),
            reason
        ]);
        Assert.NotNull(value);
        return value!;
    }

    private static async Task<object?> InvokeAsync(string methodName, params object?[] args)
    {
        var service = RequireType("PilotOperationsService");
        var method = service.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(x => x.Name == methodName && x.GetParameters().Length == args.Length);
        Assert.NotNull(method);
        var task = Assert.IsAssignableFrom<Task>(method!.Invoke(null, args));
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    private static object? Property(object value, string propertyName) => value.GetType().GetProperty(propertyName)?.GetValue(value);

    private static string? ReadPatternKey(string evidenceJson)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(evidenceJson);
        return doc.RootElement.GetProperty("patternKey").GetString();
    }

    private static AtlasDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase($"vs35-prepare-{Guid.NewGuid():N}")
            .Options;
        return new AtlasDbContext(options);
    }

    private static SeededBusiness SeedBusiness(AtlasDbContext db, string category, string goalType, string? contextKey = null, string? contextValue = null)
    {
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var account = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = $"owner-{Guid.NewGuid():N}", CreatedAt = Now };
        var business = new Business
        {
            Id = Guid.NewGuid(), Name = "Pilot Business", Category = category, Country = "MT", Timezone = "Europe/Malta",
            Currency = "EUR", PrimaryLocation = "Valletta, Malta", OperatingStatus = "Open", CreatedAt = Now
        };
        var profile = new BusinessProfile { BusinessId = business.Id, Language = "en", Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = Now };
        var goal = new BusinessGoal { Id = Guid.NewGuid(), BusinessId = business.Id, Type = goalType, Title = "Grow revenue", Priority = 1, UpdatedAt = Now };
        var assignment = new BusinessKnowledgeAssignment
        {
            Id = Guid.NewGuid(), BusinessId = business.Id, KnowledgePackId = Guid.NewGuid(), KnowledgePackVersionId = Guid.NewGuid(),
            PackKey = core.PackKey, ExactVersion = core.ExactVersion, IsCurrent = true,
            AssignedByUserAccountId = account.Id, AssignedAt = Now, EffectiveAt = Now
        };
        db.UserAccounts.Add(account);
        db.Businesses.Add(business);
        db.BusinessProfiles.Add(profile);
        db.BusinessGoals.Add(goal);
        db.BusinessKnowledgeAssignments.Add(assignment);
        db.BusinessMemberships.Add(new BusinessMembership
        {
            Id = Guid.NewGuid(), BusinessId = business.Id, UserAccountId = account.Id, UserAccount = account,
            Role = MembershipRoles.BusinessOwner, CreatedAt = Now
        });
        if (contextKey is not null && contextValue is not null)
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
        Id = Guid.NewGuid(), BusinessId = setup.Business.Id, GoalId = setup.Goal.Id, Title = "Existing focus",
        WhyItMatters = "Existing", WhyNow = "Existing", ExpectedImpact = "Existing", Effort = "Low", Confidence = "Medium",
        EvidenceSummary = "Existing", EvidenceJson = "{}", Status = OpportunityStatuses.Available,
        KnowledgePackKey = setup.Assignment.PackKey, KnowledgePackVersion = setup.Assignment.ExactVersion,
        KnowledgePackVersionId = setup.Assignment.KnowledgePackVersionId, CreatedAt = Now.AddHours(-1), ExpiresAt = expiresAt
    };

    private static Type RequireType(string name)
    {
        var type = ApiAssembly.GetType($"Atlas.Api.{name}");
        Assert.NotNull(type);
        return type!;
    }

    private sealed record SeededBusiness(UserAccount Account, Business Business, BusinessProfile Profile, BusinessGoal Goal, BusinessKnowledgeAssignment Assignment);
}
