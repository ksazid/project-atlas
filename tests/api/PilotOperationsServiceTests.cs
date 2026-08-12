using System.Collections;
using System.Reflection;
using System.Security.Claims;
using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class PilotOperationsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 12, 30, 0, TimeSpan.Zero);
    private static Assembly ApiAssembly => typeof(AtlasDbContext).Assembly;

    [Fact]
    public async Task Internal_identity_is_resolved_without_business_membership()
    {
        await using var db = CreateDb();
        var user = Principal("pilot-operator-subject");
        var account = await InvokeAsync("ResolveOperatorAsync", db, user, Now, CancellationToken.None);

        Assert.NotNull(account);
        var accountId = Assert.IsType<Guid>(Property(account!, "Id"));
        Assert.Equal("pilot-operator-subject", Property(account!, "ProviderSubject"));
        Assert.Equal(accountId, (await db.UserAccounts.SingleAsync()).Id);
        Assert.Empty(await db.BusinessMemberships.ToListAsync());

        var same = await InvokeAsync("ResolveOperatorAsync", db, user, Now.AddMinutes(1), CancellationToken.None);
        Assert.Equal(accountId, Assert.IsType<Guid>(Property(same!, "Id")));
        Assert.Single(await db.UserAccounts.ToListAsync());
    }

    [Fact]
    public async Task Support_notes_append_and_are_audited()
    {
        await using var db = CreateDb();
        var business = SeedBusiness(db);
        var account = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "operator", CreatedAt = Now };
        db.UserAccounts.Add(account);
        await db.SaveChangesAsync();

        var requestType = RequireType("PilotSupportNoteRequest");
        var first = Activator.CreateInstance(requestType, [" First follow-up. "])!;
        var second = Activator.CreateInstance(requestType, ["Second follow-up."])!;

        Assert.NotNull(await InvokeAsync("AddSupportNoteAsync", db, business.Id, account, first, Now, CancellationToken.None));
        Assert.NotNull(await InvokeAsync("AddSupportNoteAsync", db, business.Id, account, second, Now.AddMinutes(1), CancellationToken.None));

        var notes = await db.PilotOperationRecords.OrderBy(x => x.OccurredAt).ToListAsync();
        Assert.Equal(2, notes.Count);
        Assert.All(notes, x => Assert.Equal(PilotOperationActions.SupportNote, x.Action));
        Assert.Equal("First follow-up.", notes[0].Reason);
        Assert.Equal("Second follow-up.", notes[1].Reason);
        Assert.Equal(2, await db.AuditRecords.CountAsync(x => x.BusinessId == business.Id));
    }

    [Fact]
    public async Task Operator_profile_correction_clears_owner_confirmation_and_records_provenance()
    {
        await using var db = CreateDb();
        var business = SeedBusiness(db);
        var account = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "operator", CreatedAt = Now };
        var profile = new BusinessProfile
        {
            BusinessId = business.Id,
            Description = "Old description",
            Address = "Old address",
            Website = "https://example.test",
            Phone = "+35600000000",
            Email = "old@example.test",
            SocialChannels = "instagram",
            BusinessHours = "Mon-Fri 09:00-17:00",
            Language = "en",
            Source = FieldSources.Owner,
            OwnerConfirmed = true,
            UpdatedAt = Now.AddDays(-1)
        };
        db.UserAccounts.Add(account);
        db.BusinessProfiles.Add(profile);
        await db.SaveChangesAsync();

        var requestType = RequireType("PilotProfileCorrectionRequest");
        var request = Activator.CreateInstance(requestType, [
            "Corrected description",
            "Old address",
            "https://example.test",
            "+35600000000",
            "new@example.test",
            "instagram",
            "Mon-Fri 09:00-17:00",
            "en",
            "Owner reported outdated public details."
        ])!;

        var result = await InvokeAsync("CorrectProfileAsync", db, business.Id, account, request, Now, CancellationToken.None);
        Assert.NotNull(result);

        var saved = await db.BusinessProfiles.SingleAsync();
        Assert.Equal("Corrected description", saved.Description);
        Assert.Equal("new@example.test", saved.Email);
        Assert.Equal(FieldSources.OperatorAssisted, saved.Source);
        Assert.False(saved.OwnerConfirmed);
        Assert.Equal(Now, saved.UpdatedAt);

        var operation = await db.PilotOperationRecords.SingleAsync();
        Assert.Equal(PilotOperationActions.ProfileCorrection, operation.Action);
        Assert.Equal("Owner reported outdated public details.", operation.Reason);
        Assert.Contains("Description", operation.MetadataJson ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("Email", operation.MetadataJson ?? string.Empty, StringComparison.Ordinal);
        Assert.Single(await db.AuditRecords.Where(x => x.BusinessId == business.Id).ToListAsync());
    }

    [Fact]
    public async Task Queue_exposes_real_attention_indicators_without_synthetic_score()
    {
        await using var db = CreateDb();
        var business = SeedBusiness(db);
        var owner = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "owner", CreatedAt = Now };
        db.UserAccounts.Add(owner);
        db.BusinessProfiles.Add(new BusinessProfile { BusinessId = business.Id, Language = "en", Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = Now });
        db.BusinessGoals.Add(new BusinessGoal { Id = Guid.NewGuid(), BusinessId = business.Id, Type = "revenue", Title = "Grow revenue", Priority = 1, UpdatedAt = Now });
        db.IntelligenceRuns.Add(new IntelligenceRunRecord { Id = Guid.NewGuid(), BusinessId = business.Id, ActorUserAccountId = owner.Id, Outcome = OpportunityFocusGenerationStates.Degraded, Code = "core_manifest_unavailable", CandidateCount = 0, OccurredAt = Now });
        db.FeedbackRecords.AddRange(
            new FeedbackRecord { Id = Guid.NewGuid(), BusinessId = business.Id, SubmittedByAccountId = owner.Id, Kind = FeedbackKinds.UnsafeGuidance, Message = "Unsafe", CreatedAt = Now },
            new FeedbackRecord { Id = Guid.NewGuid(), BusinessId = business.Id, SubmittedByAccountId = owner.Id, Kind = FeedbackKinds.OpportunityRating, Usefulness = FeedbackUsefulnessValues.Useful, OpportunityId = null, CreatedAt = Now },
            new FeedbackRecord { Id = Guid.NewGuid(), BusinessId = business.Id, SubmittedByAccountId = owner.Id, Kind = FeedbackKinds.OpportunityRating, Usefulness = FeedbackUsefulnessValues.NotUseful, OpportunityId = null, CreatedAt = Now });
        await db.SaveChangesAsync();

        var result = await InvokeAsync("ListBusinessesAsync", db, CancellationToken.None);
        var items = Assert.IsAssignableFrom<IEnumerable>(result).Cast<object>().ToList();
        var item = Assert.Single(items);
        Assert.Equal(business.Id, Property(item, "BusinessId"));
        Assert.Equal(true, Property(item, "ProfileConfirmed"));
        Assert.Equal(1, Property(item, "GoalCount"));
        Assert.Equal(OpportunityFocusGenerationStates.Degraded, Property(item, "LatestGenerationOutcome"));
        Assert.Equal(1, Property(item, "UnsafeFeedbackCount"));
        Assert.Equal(1, Property(item, "UsefulFeedbackCount"));
        Assert.Equal(1, Property(item, "NotUsefulFeedbackCount"));
        Assert.Null(item.GetType().GetProperty("QualityScore"));
    }

    private static Business SeedBusiness(AtlasDbContext db)
    {
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Pilot Business",
            Category = "restaurant-cafe",
            Country = "MT",
            Timezone = "Europe/Malta",
            Currency = "EUR",
            PrimaryLocation = "Valletta, Malta",
            OperatingStatus = "Open",
            CreatedAt = Now
        };
        db.Businesses.Add(business);
        return business;
    }

    private static ClaimsPrincipal Principal(string subject) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, subject),
        new Claim(ClaimTypes.Role, MembershipRoles.PilotOperator)
    ], "test"));

    private static AtlasDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase($"pilot-operations-{Guid.NewGuid():N}")
            .Options;
        return new AtlasDbContext(options);
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

    private static object? Property(object value, string propertyName) =>
        value.GetType().GetProperty(propertyName)?.GetValue(value);

    private static Type RequireType(string name)
    {
        var type = ApiAssembly.GetType($"Atlas.Api.{name}");
        Assert.NotNull(type);
        return type!;
    }
}
