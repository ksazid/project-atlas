using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class KnowledgePackPolicyTests
{
    [Fact]
    public void Knowledge_section_metadata_maps_to_migrated_jsonb_column()
    {
        using var db = new AtlasDbContext(new DbContextOptionsBuilder<AtlasDbContext>()
            .UseNpgsql("Host=localhost;Database=atlas;Username=postgres;Password=postgres")
            .Options);

        var entity = db.Model.FindEntityType(typeof(KnowledgeSection));
        var property = entity?.FindProperty(nameof(KnowledgeSection.MetadataJson));

        Assert.NotNull(property);
        Assert.Equal("jsonb", property.GetColumnType());
    }

    [Fact]
    public void Generic_pack_is_published_modular_and_industry_agnostic()
    {
        var actorId = Guid.NewGuid();
        var (pack, version) = GenericBusinessKnowledgePack.Create(actorId);

        Assert.Equal(KnowledgePackKeys.GenericBusiness, pack.Key);
        Assert.Equal(GenericBusinessKnowledgePack.InitialVersion, version.VersionNumber);
        Assert.Equal(KnowledgePackStatuses.Published, version.Status);
        Assert.Equal(3, version.Sections.Count);
        Assert.Equal([1, 2, 3], version.Sections.OrderBy(x => x.Order).Select(x => x.Order).ToArray());
        Assert.DoesNotContain(version.Sections, x => x.Content.Contains("restaurant", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(KnowledgePackStatuses.Draft, KnowledgePackStatuses.Review, true)]
    [InlineData(KnowledgePackStatuses.Review, KnowledgePackStatuses.Draft, true)]
    [InlineData(KnowledgePackStatuses.Review, KnowledgePackStatuses.Published, true)]
    [InlineData(KnowledgePackStatuses.Published, KnowledgePackStatuses.Archived, true)]
    [InlineData(KnowledgePackStatuses.Draft, KnowledgePackStatuses.Published, false)]
    [InlineData(KnowledgePackStatuses.Published, KnowledgePackStatuses.Draft, false)]
    public void Lifecycle_transitions_are_explicit(string from, string to, bool expected)
    {
        Assert.Equal(expected, KnowledgePackStatuses.CanTransition(from, to));
    }

    [Fact]
    public void Publishing_requires_at_least_one_section()
    {
        var version = Version(KnowledgePackStatuses.Review);
        Assert.Throws<InvalidOperationException>(() => version.TransitionTo(KnowledgePackStatuses.Published, Guid.NewGuid()));
    }

    [Fact]
    public void Published_and_archived_versions_are_immutable()
    {
        Assert.Throws<InvalidOperationException>(() => Version(KnowledgePackStatuses.Published).EnsureEditable());
        Assert.Throws<InvalidOperationException>(() => Version(KnowledgePackStatuses.Archived).EnsureEditable());
        Version(KnowledgePackStatuses.Draft).EnsureEditable();
        Version(KnowledgePackStatuses.Review).EnsureEditable();
    }

    [Fact]
    public void Assignment_requires_and_retains_exact_published_version()
    {
        var actorId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var (pack, version) = GenericBusinessKnowledgePack.Create(actorId);
        var assignment = BusinessKnowledgeAssignment.Assign(businessId, pack, version, actorId);

        Assert.Equal(pack.Key, assignment.PackKey);
        Assert.Equal(version.VersionNumber, assignment.ExactVersion);
        Assert.Equal(version.Id, assignment.KnowledgePackVersionId);
        Assert.True(assignment.IsCurrent);

        version.Status = KnowledgePackStatuses.Draft;
        Assert.Throws<InvalidOperationException>(() => BusinessKnowledgeAssignment.Assign(businessId, pack, version, actorId));
    }

    [Fact]
    public async Task Current_assignments_are_business_isolated()
    {
        await using var db = new AtlasDbContext(new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var actorId = Guid.NewGuid();
        var first = Business.Create(new CreateBusinessRequest("First", "Cafe", "Malta", "Europe/Malta", "EUR", "Balzan", "Open"));
        var second = Business.Create(new CreateBusinessRequest("Second", "Retail", "Malta", "Europe/Malta", "EUR", "Sliema", "Open"));
        var (pack, version) = GenericBusinessKnowledgePack.Create(actorId);
        db.AddRange(first, second, pack,
            BusinessKnowledgeAssignment.Assign(first.Id, pack, version, actorId),
            BusinessKnowledgeAssignment.Assign(second.Id, pack, version, actorId));
        await db.SaveChangesAsync();

        var firstAssignments = await db.BusinessKnowledgeAssignments
            .Where(x => x.BusinessId == first.Id && x.IsCurrent)
            .ToListAsync();

        Assert.Single(firstAssignments);
        Assert.All(firstAssignments, x => Assert.Equal(first.Id, x.BusinessId));
    }

    private static KnowledgePackVersion Version(string status) => new()
    {
        Id = Guid.NewGuid(),
        KnowledgePackId = Guid.NewGuid(),
        VersionNumber = "2.0",
        Status = status,
        Locale = "en",
        CreatedByUserAccountId = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow
    };
}
