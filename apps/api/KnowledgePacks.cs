using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static partial class KnowledgePackKeys
{
    public const string GenericBusiness = "generic-business";

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 80 && KeyPattern().IsMatch(value);
}

public static class KnowledgePackStatuses
{
    public const string Draft = "draft";
    public const string Review = "review";
    public const string Published = "published";
    public const string Archived = "archived";

    public static bool CanTransition(string from, string to) => (from, to) switch
    {
        (Draft, Review) => true,
        (Review, Draft) => true,
        (Review, Published) => true,
        (Published, Archived) => true,
        _ => false
    };
}

public static class KnowledgeSectionCategories
{
    public const string BusinessOverview = "business-overview";
    public const string Services = "services";
    public const string Faqs = "faqs";
    public const string BrandVoice = "brand-voice";
    public const string Policies = "policies";
    public const string Pricing = "pricing";
    public const string Promotions = "promotions";
    public const string SalesGuidance = "sales-guidance";
    public const string Sops = "sops";
    public const string Custom = "custom";
}

[Index(nameof(Key), IsUnique = true)]
public sealed class KnowledgePack
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsArchived { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public uint Version { get; set; }
    public ICollection<KnowledgePackVersion> Versions { get; set; } = [];
}

[Index(nameof(KnowledgePackId), nameof(VersionNumber), IsUnique = true)]
public sealed class KnowledgePackVersion
{
    public Guid Id { get; set; }
    public Guid KnowledgePackId { get; set; }
    public required string VersionNumber { get; set; }
    public required string Status { get; set; }
    public required string Locale { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? ReviewedByUserAccountId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? PublishedByUserAccountId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public uint ConcurrencyVersion { get; set; }
    public KnowledgePack KnowledgePack { get; set; } = null!;
    public ICollection<KnowledgeSection> Sections { get; set; } = [];

    public void TransitionTo(string nextStatus, Guid actorId)
    {
        if (!KnowledgePackStatuses.CanTransition(Status, nextStatus))
            throw new InvalidOperationException($"Knowledge Pack version cannot transition from {Status} to {nextStatus}.");
        if (nextStatus == KnowledgePackStatuses.Published && Sections.Count == 0)
            throw new InvalidOperationException("A Knowledge Pack version requires at least one section before publication.");

        Status = nextStatus;
        var now = DateTimeOffset.UtcNow;
        if (nextStatus == KnowledgePackStatuses.Review) { ReviewedByUserAccountId = actorId; ReviewedAt = now; }
        if (nextStatus == KnowledgePackStatuses.Published) { PublishedByUserAccountId = actorId; PublishedAt = now; }
        if (nextStatus == KnowledgePackStatuses.Archived) ArchivedAt = now;
    }

    public void EnsureEditable()
    {
        if (Status is KnowledgePackStatuses.Published or KnowledgePackStatuses.Archived)
            throw new InvalidOperationException("Published and archived Knowledge Pack versions are immutable.");
    }
}

[Index(nameof(KnowledgePackVersionId), nameof(StableKey), IsUnique = true)]
[Index(nameof(KnowledgePackVersionId), nameof(Order), IsUnique = true)]
public sealed class KnowledgeSection
{
    public Guid Id { get; set; }
    public Guid KnowledgePackVersionId { get; set; }
    public required string StableKey { get; set; }
    public required string Category { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public string? MetadataJson { get; set; }
    public int Order { get; set; }
    public required string Locale { get; set; }
    public string? TranslationGroupKey { get; set; }
    public string? Source { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint ConcurrencyVersion { get; set; }
    public KnowledgePackVersion KnowledgePackVersion { get; set; } = null!;
}

[Index(nameof(BusinessId), nameof(IsCurrent), IsUnique = true)]
public sealed class BusinessKnowledgeAssignment
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid KnowledgePackId { get; set; }
    public Guid KnowledgePackVersionId { get; set; }
    public required string PackKey { get; set; }
    public required string ExactVersion { get; set; }
    public bool IsCurrent { get; set; }
    public Guid AssignedByUserAccountId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public uint ConcurrencyVersion { get; set; }
    public Business Business { get; set; } = null!;
    public KnowledgePack KnowledgePack { get; set; } = null!;
    public KnowledgePackVersion KnowledgePackVersion { get; set; } = null!;

    public static BusinessKnowledgeAssignment Assign(Guid businessId, KnowledgePack pack, KnowledgePackVersion version, Guid actorId) =>
        version.Status != KnowledgePackStatuses.Published
            ? throw new InvalidOperationException("Only published Knowledge Pack versions can be assigned.")
            : new BusinessKnowledgeAssignment
            {
                Id = Guid.NewGuid(), BusinessId = businessId, KnowledgePackId = pack.Id,
                KnowledgePackVersionId = version.Id, PackKey = pack.Key, ExactVersion = version.VersionNumber,
                IsCurrent = true, AssignedByUserAccountId = actorId, AssignedAt = DateTimeOffset.UtcNow,
                EffectiveAt = DateTimeOffset.UtcNow
            };
}

public sealed record KnowledgeSectionResponse(Guid Id, string StableKey, string Category, string Title, string Content, string? MetadataJson, int Order, string Locale);
public sealed record KnowledgePackResponse(string Key, string Name, string Description, string Version, string Status, string Locale, IReadOnlyList<KnowledgeSectionResponse> Sections, DateTimeOffset AssignedAt);

public static class GenericBusinessKnowledgePack
{
    public const string InitialVersion = "1.0";

    public static (KnowledgePack Pack, KnowledgePackVersion Version) Create(Guid actorId)
    {
        var pack = new KnowledgePack
        {
            Id = Guid.NewGuid(), Key = KnowledgePackKeys.GenericBusiness, Name = "Generic Business",
            Description = "Industry-agnostic knowledge for practical business guidance.",
            CreatedByUserAccountId = actorId, CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new KnowledgePackVersion
        {
            Id = Guid.NewGuid(), KnowledgePackId = pack.Id, KnowledgePack = pack,
            VersionNumber = InitialVersion, Status = KnowledgePackStatuses.Published, Locale = "en",
            CreatedByUserAccountId = actorId, CreatedAt = DateTimeOffset.UtcNow,
            PublishedByUserAccountId = actorId, PublishedAt = DateTimeOffset.UtcNow
        };
        version.Sections =
        [
            new KnowledgeSection { Id = Guid.NewGuid(), KnowledgePackVersionId = version.Id, KnowledgePackVersion = version, StableKey = "business-overview", Category = KnowledgeSectionCategories.BusinessOverview, Title = "Business Overview", Content = "Use the confirmed business profile, goals and context as the primary evidence base.", Order = 1, Locale = "en", Source = "system", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new KnowledgeSection { Id = Guid.NewGuid(), KnowledgePackVersionId = version.Id, KnowledgePackVersion = version, StableKey = "sales-guidance", Category = KnowledgeSectionCategories.SalesGuidance, Title = "Sales Guidance", Content = "Prefer evidence-backed, practical actions across revenue, retention, efficiency, customer experience and risk reduction.", Order = 2, Locale = "en", Source = "system", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new KnowledgeSection { Id = Guid.NewGuid(), KnowledgePackVersionId = version.Id, KnowledgePackVersion = version, StableKey = "guardrails", Category = KnowledgeSectionCategories.Policies, Title = "Guidance Guardrails", Content = "Respect business boundaries, avoid unsupported claims and retain the exact assigned Knowledge Pack version.", Order = 3, Locale = "en", Source = "system", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        ];
        pack.Versions.Add(version);
        return (pack, version);
    }
}
