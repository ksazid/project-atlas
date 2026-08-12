using System.Reflection;
using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class FeedbackPersistenceTests
{
    [Fact]
    public async Task Submit_persists_business_account_kind_and_trimmed_note()
    {
        await using var db = CreateDb();
        var owner = Account("feedback-owner");
        var business = Business.Create(new CreateBusinessRequest("Harbour Coffee", "restaurant-cafe", "MT", "Europe/Malta", "EUR", "Valletta", "Open"));
        db.AddRange(owner, business);
        await db.SaveChangesAsync();

        var receipt = await Submit(db, business.Id, owner,
            new SubmitFeedbackRequest(FeedbackKinds.GeneralFeedback, null, null, null, "  helpful app  "));

        Assert.NotNull(receipt);
        var record = await db.Set<FeedbackRecord>().SingleAsync();
        Assert.Equal(business.Id, record.BusinessId);
        Assert.Equal(owner.Id, record.SubmittedByAccountId);
        Assert.Equal(FeedbackKinds.GeneralFeedback, record.Kind);
        Assert.Equal("helpful app", record.Message);
        Assert.NotEqual(default, record.CreatedAt);
    }

    [Fact]
    public async Task Opportunity_reference_must_belong_to_the_same_business()
    {
        await using var db = CreateDb();
        var owner = Account("feedback-isolation");
        var business = CreateBusiness("Owner Business");
        var otherBusiness = CreateBusiness("Other Business");
        var opportunity = OpportunityFor(otherBusiness.Id);
        db.AddRange(owner, business, otherBusiness, opportunity);
        await db.SaveChangesAsync();

        var receipt = await Submit(db, business.Id, owner,
            new SubmitFeedbackRequest(FeedbackKinds.UnsafeGuidance, opportunity.Id, null, null, "unsafe"));

        Assert.Null(receipt);
        Assert.Empty(await db.Set<FeedbackRecord>().ToListAsync());
    }

    [Fact]
    public async Task Unsafe_report_does_not_mutate_opportunity_state_or_version()
    {
        await using var db = CreateDb();
        var owner = Account("feedback-safe-mutation");
        var business = CreateBusiness("Owner Business");
        var opportunity = OpportunityFor(business.Id);
        db.AddRange(owner, business, opportunity);
        await db.SaveChangesAsync();
        var status = opportunity.Status;
        var version = opportunity.ConcurrencyVersion;
        var expiresAt = opportunity.ExpiresAt;

        var receipt = await Submit(db, business.Id, owner,
            new SubmitFeedbackRequest(FeedbackKinds.UnsafeGuidance, opportunity.Id, null, null, "Please review"));

        Assert.NotNull(receipt);
        Assert.Equal(status, opportunity.Status);
        Assert.Equal(version, opportunity.ConcurrencyVersion);
        Assert.Equal(expiresAt, opportunity.ExpiresAt);
    }

    [Fact]
    public async Task Multiple_submissions_are_append_only()
    {
        await using var db = CreateDb();
        var owner = Account("feedback-append-only");
        var business = CreateBusiness("Append Business");
        db.AddRange(owner, business);
        await db.SaveChangesAsync();

        await Submit(db, business.Id, owner, new SubmitFeedbackRequest(FeedbackKinds.GeneralFeedback, null, null, null, "one"));
        await Submit(db, business.Id, owner, new SubmitFeedbackRequest(FeedbackKinds.SupportRequest, null, null, null, "two"));

        var records = await db.Set<FeedbackRecord>().OrderBy(x => x.CreatedAt).ToListAsync();
        Assert.Equal(2, records.Count);
        Assert.NotEqual(records[0].Id, records[1].Id);
        Assert.Equal([FeedbackKinds.GeneralFeedback, FeedbackKinds.SupportRequest], records.Select(x => x.Kind).ToArray());
    }

    private static async Task<object?> Submit(AtlasDbContext db, Guid businessId, UserAccount owner, SubmitFeedbackRequest request)
    {
        var service = typeof(FeedbackPolicy).Assembly.GetType("Atlas.Api.FeedbackService");
        Assert.NotNull(service);
        var method = service!.GetMethod("SubmitAsync", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        var invoked = method!.Invoke(null, [db, businessId, owner, request, CancellationToken.None]);
        var task = Assert.IsAssignableFrom<Task>(invoked);
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    private static AtlasDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static UserAccount Account(string subject) => new()
    {
        Id = Guid.NewGuid(), ProviderSubject = subject, CreatedAt = DateTimeOffset.UtcNow
    };

    private static Business CreateBusiness(string name) => Business.Create(
        new CreateBusinessRequest(name, "restaurant-cafe", "MT", "Europe/Malta", "EUR", "Valletta", "Open"));

    private static Opportunity OpportunityFor(Guid businessId) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        KnowledgePackVersionId = Guid.NewGuid(),
        Title = "Improve lunch visibility",
        WhyItMatters = "Observed opportunity",
        WhyNow = "Owner-confirmed operating context supports this action now.",
        ExpectedImpact = "More qualified demand",
        Effort = "low",
        Confidence = "medium",
        EvidenceSummary = "Owner-confirmed context",
        EvidenceJson = "[]",
        Status = OpportunityStatuses.Available,
        KnowledgePackKey = "generic-business",
        KnowledgePackVersion = "1.0.0",
        CreatedAt = DateTimeOffset.UtcNow,
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        ConcurrencyVersion = 3
    };
}