using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class KnowledgePackPolicyTests
{
    [Fact]
    public void Generic_pack_is_valid_versioned_and_industry_agnostic()
    {
        var pack = GenericBusinessKnowledgePack.Create();

        Assert.Equal(KnowledgePackKeys.GenericBusiness, pack.Key);
        Assert.Equal("1.0", pack.Version);
        Assert.Contains("revenue", pack.Content.OpportunityThemes);
        Assert.Contains("require-evidence", pack.Content.Guardrails);
        Assert.DoesNotContain(pack.Content.OpportunityThemes, x => x.Contains("restaurant", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Generic Business")]
    [InlineData("generic_business")]
    public void Invalid_pack_keys_are_rejected(string key)
    {
        Assert.Throws<ArgumentException>(() => KnowledgePack.Publish(
            key,
            "1.0",
            "Pack",
            "Description",
            new KnowledgePackContent(["revenue"], ["profile"], ["require-evidence"])));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1.0.0")]
    [InlineData("latest")]
    public void Unsupported_versions_are_rejected(string version)
    {
        Assert.Throws<ArgumentException>(() => KnowledgePack.Publish(
            "valid-pack",
            version,
            "Pack",
            "Description",
            new KnowledgePackContent(["revenue"], ["profile"], ["require-evidence"])));
    }

    [Fact]
    public void Assignment_retains_exact_key_and_version()
    {
        var businessId = Guid.NewGuid();
        var pack = GenericBusinessKnowledgePack.Create();
        var assignment = BusinessKnowledgePack.Assign(businessId, pack);

        Assert.Equal(pack.Key, assignment.PackKey);
        Assert.Equal(pack.Version, assignment.PackVersion);
        Assert.True(assignment.IsActive);
    }

    [Fact]
    public async Task Active_assignments_are_business_isolated()
    {
        await using var db = new AtlasDbContext(new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var first = Business.Create(new CreateBusinessRequest("First", "Cafe", "Malta", "Europe/Malta", "EUR", "Balzan", "Open"));
        var second = Business.Create(new CreateBusinessRequest("Second", "Retail", "Malta", "Europe/Malta", "EUR", "Sliema", "Open"));
        var pack = GenericBusinessKnowledgePack.Create();
        db.AddRange(first, second, pack, BusinessKnowledgePack.Assign(first.Id, pack), BusinessKnowledgePack.Assign(second.Id, pack));
        await db.SaveChangesAsync();

        var firstAssignments = await db.Set<BusinessKnowledgePack>()
            .Where(x => x.BusinessId == first.Id && x.IsActive)
            .ToListAsync();

        Assert.Single(firstAssignments);
        Assert.All(firstAssignments, x => Assert.Equal(first.Id, x.BusinessId));
    }
}
