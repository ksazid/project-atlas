using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class ProgressiveQuestionPersistenceTests
{
    [Fact]
    public async Task Get_IsBusinessOwnerIsolated()
    {
        await using var db = CreateDb();
        var (owner, business) = await SeedOwnedBusiness(db, "owner-a", "restaurant-cafe");
        var other = Account("owner-b");
        db.UserAccounts.Add(other);
        await db.SaveChangesAsync();

        var result = await ProgressiveQuestionService.GetAsync(db, owner.ProviderSubject, business.Id, CancellationToken.None);
        Assert.NotEmpty(result.Questions);
        Assert.Equal(ProgressiveQuestionCatalogueV1.CatalogueKey, result.CatalogueKey);
        Assert.Equal(ProgressiveQuestionCatalogueV1.Version, result.CatalogueVersion);

        var foreign = await Assert.ThrowsAsync<ProgressiveQuestionException>(() =>
            ProgressiveQuestionService.GetAsync(db, other.ProviderSubject, business.Id, CancellationToken.None));
        Assert.Equal("progressive_questions_not_found", foreign.Code);
    }

    [Fact]
    public async Task Answer_WritesOwnerConfirmedContextAndProgressAtomically()
    {
        await using var db = CreateDb();
        var (owner, business) = await SeedOwnedBusiness(db, "owner-a", "generic-business");

        var result = await ProgressiveQuestionService.AnswerAsync(
            db,
            owner.ProviderSubject,
            business.Id,
            "generic.primary-channel",
            new ProgressiveQuestionAnswerRequest(ProgressiveQuestionCatalogueV1.Version, ["In person"], null),
            CancellationToken.None);

        var context = await db.BusinessContextEntries.SingleAsync(x => x.BusinessId == business.Id && x.Key == "primarychannels");
        Assert.Equal("In person", context.Value);
        Assert.Equal(FieldSources.Owner, context.Source);
        Assert.True(context.OwnerConfirmed);

        var progress = await db.BusinessQuestionProgress.SingleAsync(x => x.BusinessId == business.Id && x.QuestionKey == "generic.primary-channel");
        Assert.Equal(BusinessQuestionProgressStatuses.Answered, progress.Status);
        Assert.Equal("primarychannels", progress.AnsweredContextKey);
        Assert.Equal("answered", result.Status);
        Assert.DoesNotContain(result.Remaining.Questions, x => x.QuestionKey == "generic.primary-channel");
        Assert.Contains(await db.AuditRecords.Where(x => x.BusinessId == business.Id).ToListAsync(), x => x.Action == "business.progressive-question.answered:generic.primary-channel");
    }

    [Fact]
    public async Task Skip_WritesProgressButNeverCreatesFakeContext()
    {
        await using var db = CreateDb();
        var (owner, business) = await SeedOwnedBusiness(db, "owner-a", "generic-business");

        var result = await ProgressiveQuestionService.SkipAsync(
            db,
            owner.ProviderSubject,
            business.Id,
            "generic.primary-constraint",
            ProgressiveQuestionCatalogueV1.Version,
            CancellationToken.None);

        Assert.Empty(await db.BusinessContextEntries.Where(x => x.BusinessId == business.Id && x.Key == "constraints").ToListAsync());
        var progress = await db.BusinessQuestionProgress.SingleAsync(x => x.BusinessId == business.Id && x.QuestionKey == "generic.primary-constraint");
        Assert.Equal(BusinessQuestionProgressStatuses.Skipped, progress.Status);
        Assert.Null(progress.AnsweredContextKey);
        Assert.Equal("skipped", result.Status);
        Assert.Contains(await db.AuditRecords.Where(x => x.BusinessId == business.Id).ToListAsync(), x => x.Action == "business.progressive-question.skipped:generic.primary-constraint");
    }

    [Fact]
    public async Task RepeatingSameAnswerIsStableAndDoesNotDuplicateRows()
    {
        await using var db = CreateDb();
        var (owner, business) = await SeedOwnedBusiness(db, "owner-a", "generic-business");
        var request = new ProgressiveQuestionAnswerRequest(ProgressiveQuestionCatalogueV1.Version, ["In person"], null);

        await ProgressiveQuestionService.AnswerAsync(db, owner.ProviderSubject, business.Id, "generic.primary-channel", request, CancellationToken.None);
        var second = await ProgressiveQuestionService.AnswerAsync(db, owner.ProviderSubject, business.Id, "generic.primary-channel", request, CancellationToken.None);

        Assert.Equal("answered", second.Status);
        Assert.Single(await db.BusinessContextEntries.Where(x => x.BusinessId == business.Id && x.Key == "primarychannels").ToListAsync());
        Assert.Single(await db.BusinessQuestionProgress.Where(x => x.BusinessId == business.Id && x.QuestionKey == "generic.primary-channel").ToListAsync());
    }

    [Fact]
    public async Task ServiceRejectsStaleVersionUnknownQuestionAndInvalidChoices()
    {
        await using var db = CreateDb();
        var (owner, business) = await SeedOwnedBusiness(db, "owner-a", "generic-business");

        var stale = await Assert.ThrowsAsync<ProgressiveQuestionException>(() => ProgressiveQuestionService.AnswerAsync(
            db, owner.ProviderSubject, business.Id, "generic.primary-channel",
            new ProgressiveQuestionAnswerRequest("0", ["In person"], null), CancellationToken.None));
        Assert.Equal("progressive_catalogue_stale", stale.Code);

        var unknown = await Assert.ThrowsAsync<ProgressiveQuestionException>(() => ProgressiveQuestionService.SkipAsync(
            db, owner.ProviderSubject, business.Id, "client.injected-question", ProgressiveQuestionCatalogueV1.Version, CancellationToken.None));
        Assert.Equal("progressive_question_not_found", unknown.Code);

        var invalid = await Assert.ThrowsAsync<ProgressiveQuestionValidationException>(() => ProgressiveQuestionService.AnswerAsync(
            db, owner.ProviderSubject, business.Id, "generic.primary-channel",
            new ProgressiveQuestionAnswerRequest(ProgressiveQuestionCatalogueV1.Version, ["Invented channel"], null), CancellationToken.None));
        Assert.Contains(nameof(ProgressiveQuestionAnswerRequest.Selections), invalid.Errors.Keys);
    }

    [Fact]
    public async Task MultiChoiceRejectsDuplicatesAndTooManySelections_AndShortTextIsBounded()
    {
        await using var db = CreateDb();
        var (owner, business) = await SeedOwnedBusiness(db, "owner-a", "generic-business");

        var duplicates = await Assert.ThrowsAsync<ProgressiveQuestionValidationException>(() => ProgressiveQuestionService.AnswerAsync(
            db, owner.ProviderSubject, business.Id, "generic.primary-channel",
            new ProgressiveQuestionAnswerRequest(ProgressiveQuestionCatalogueV1.Version, ["In person", "In person"], null), CancellationToken.None));
        Assert.Contains(nameof(ProgressiveQuestionAnswerRequest.Selections), duplicates.Errors.Keys);

        var tooMany = await Assert.ThrowsAsync<ProgressiveQuestionValidationException>(() => ProgressiveQuestionService.AnswerAsync(
            db, owner.ProviderSubject, business.Id, "generic.primary-channel",
            new ProgressiveQuestionAnswerRequest(ProgressiveQuestionCatalogueV1.Version, ["In person", "Phone/message", "Own website/app", "Marketplace/platform"], null), CancellationToken.None));
        Assert.Contains(nameof(ProgressiveQuestionAnswerRequest.Selections), tooMany.Errors.Keys);

        var tooLong = await Assert.ThrowsAsync<ProgressiveQuestionValidationException>(() => ProgressiveQuestionService.AnswerAsync(
            db, owner.ProviderSubject, business.Id, "generic.customer-groups",
            new ProgressiveQuestionAnswerRequest(ProgressiveQuestionCatalogueV1.Version, null, new string('x', 241)), CancellationToken.None));
        Assert.Contains(nameof(ProgressiveQuestionAnswerRequest.Text), tooLong.Errors.Keys);
    }

    private static async Task<(UserAccount Owner, Business Business)> SeedOwnedBusiness(AtlasDbContext db, string subject, string category)
    {
        var owner = Account(subject);
        var business = Business.Create(new CreateBusinessRequest(
            "Atlas Test Business", category, "MT", "Europe/Malta", "EUR", "Valletta", "Open"));
        db.UserAccounts.Add(owner);
        db.Businesses.Add(business);
        db.BusinessMemberships.Add(new BusinessMembership
        {
            Id = Guid.NewGuid(), BusinessId = business.Id, UserAccountId = owner.Id, UserAccount = owner,
            Role = MembershipRoles.BusinessOwner, CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return (owner, business);
    }

    private static AtlasDbContext CreateDb() => new(new DbContextOptionsBuilder<AtlasDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static UserAccount Account(string subject) => new() { Id = Guid.NewGuid(), ProviderSubject = subject, CreatedAt = DateTimeOffset.UtcNow };
}
