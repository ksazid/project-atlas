using System.Reflection;
using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class PilotOpportunityWithdrawalTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 17, 0, 0, TimeSpan.Zero);
    private static Assembly ApiAssembly => typeof(AtlasDbContext).Assembly;

    [Fact]
    public void Withdrawal_status_contract_is_explicit()
    {
        var type = RequireType("PilotOpportunityStatuses");
        var field = type.GetField("Withdrawn", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(field);
        Assert.Equal("withdrawn", field!.GetValue(null));
    }

    [Fact]
    public async Task Explicit_withdrawal_is_terminal_audited_and_owner_non_actionable()
    {
        await using var db = CreateDb();
        var business = Business();
        var operatorAccount = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "operator", CreatedAt = Now };
        var opportunity = Opportunity(business.Id, Now.AddHours(2));
        db.Businesses.Add(business);
        db.UserAccounts.Add(operatorAccount);
        db.Opportunities.Add(opportunity);
        await db.SaveChangesAsync();

        var result = await InvokeWithdraw(db, business.Id, opportunity.Id, operatorAccount, new PilotWithdrawRequest("Unsafe claim in the recommendation.", opportunity.ConcurrencyVersion));

        Assert.NotNull(result);
        Assert.Equal("withdrawn", Property(result!, "State"));
        Assert.Equal("withdrawn", opportunity.Status);
        Assert.False(OpportunityPolicy.CanDecide(opportunity, Now));
        Assert.Equal("withdrawn", OpportunityPolicy.StatusFor(opportunity, Now));
        var operation = await db.PilotOperationRecords.SingleAsync();
        Assert.Equal(PilotOperationActions.OpportunityWithdrawn, operation.Action);
        Assert.Equal(opportunity.Id, operation.TargetId);
        Assert.Equal("Unsafe claim in the recommendation.", operation.Reason);
        Assert.Single(await db.AuditRecords.Where(x => x.BusinessId == business.Id).ToListAsync());

        var second = await InvokeWithdraw(db, business.Id, opportunity.Id, operatorAccount, new PilotWithdrawRequest("Try again.", opportunity.ConcurrencyVersion));
        Assert.Equal("conflict", Property(second!, "State"));
        Assert.Single(await db.PilotOperationRecords.ToListAsync());
    }

    [Fact]
    public async Task Withdrawal_rejects_stale_version_and_cross_business_target_without_mutation()
    {
        await using var db = CreateDb();
        var first = Business();
        var second = Business();
        var operatorAccount = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "operator", CreatedAt = Now };
        var opportunity = Opportunity(first.Id, Now.AddHours(2));
        db.AddRange(first, second, operatorAccount, opportunity);
        await db.SaveChangesAsync();

        var stale = await InvokeWithdraw(db, first.Id, opportunity.Id, operatorAccount, new PilotWithdrawRequest("Review finding.", opportunity.ConcurrencyVersion + 1));
        Assert.Equal("stale", Property(stale!, "State"));
        Assert.Equal(OpportunityStatuses.Available, opportunity.Status);

        var cross = await InvokeWithdraw(db, second.Id, opportunity.Id, operatorAccount, new PilotWithdrawRequest("Wrong Business.", opportunity.ConcurrencyVersion));
        Assert.Equal("not-found", Property(cross!, "State"));
        Assert.Equal(OpportunityStatuses.Available, opportunity.Status);
        Assert.Empty(await db.PilotOperationRecords.ToListAsync());
    }

    [Fact]
    public async Task Unsafe_feedback_alone_never_withdraws_an_opportunity()
    {
        await using var db = CreateDb();
        var business = Business();
        var owner = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = "owner", CreatedAt = Now };
        var opportunity = Opportunity(business.Id, Now.AddHours(2));
        db.AddRange(business, owner, opportunity);
        db.FeedbackRecords.Add(new FeedbackRecord
        {
            Id = Guid.NewGuid(), BusinessId = business.Id, SubmittedByAccountId = owner.Id,
            Kind = FeedbackKinds.UnsafeGuidance, OpportunityId = opportunity.Id, Message = "Please review.", CreatedAt = Now
        });
        await db.SaveChangesAsync();

        Assert.Equal(OpportunityStatuses.Available, opportunity.Status);
        Assert.True(OpportunityPolicy.CanDecide(opportunity, Now));
        Assert.Empty(await db.PilotOperationRecords.ToListAsync());
    }

    private static async Task<object?> InvokeWithdraw(AtlasDbContext db, Guid businessId, Guid opportunityId, UserAccount account, PilotWithdrawRequest request)
    {
        var service = RequireType("PilotOperationsService");
        var method = service.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(x => x.Name == "WithdrawOpportunityAsync" && x.GetParameters().Length == 7);
        Assert.NotNull(method);
        var task = Assert.IsAssignableFrom<Task>(method!.Invoke(null, [db, businessId, opportunityId, account, request, Now, CancellationToken.None]));
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    private static object? Property(object value, string name) => value.GetType().GetProperty(name)?.GetValue(value);

    private static AtlasDbContext CreateDb() => new(new DbContextOptionsBuilder<AtlasDbContext>()
        .UseInMemoryDatabase($"vs35-withdraw-{Guid.NewGuid():N}").Options);

    private static Business Business() => new()
    {
        Id = Guid.NewGuid(), Name = "Pilot Business", Category = "restaurant-cafe", Country = "MT", Timezone = "Europe/Malta",
        Currency = "EUR", PrimaryLocation = "Valletta, Malta", OperatingStatus = "Open", CreatedAt = Now
    };

    private static Opportunity Opportunity(Guid businessId, DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, Title = "Review next action", WhyItMatters = "Reason", WhyNow = "Now",
        ExpectedImpact = "Impact", Effort = "Low", Confidence = "Medium", EvidenceSummary = "Evidence", EvidenceJson = "{}",
        Status = OpportunityStatuses.Available, KnowledgePackKey = "generic-business", KnowledgePackVersion = "1.0",
        KnowledgePackVersionId = Guid.NewGuid(), CreatedAt = Now, ExpiresAt = expiresAt
    };

    private static Type RequireType(string name)
    {
        var type = ApiAssembly.GetType($"Atlas.Api.{name}");
        Assert.NotNull(type);
        return type!;
    }
}
