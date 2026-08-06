using System.Text.Json;
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

public static class KnowledgePackVersions
{
    public static bool IsValid(string? value) =>
        Version.TryParse(value, out var parsed) && parsed.Major >= 1 && parsed.Build < 0 && parsed.Revision < 0;
}

public sealed record KnowledgePackContent(
    IReadOnlyList<string> OpportunityThemes,
    IReadOnlyList<string> EvidenceSignals,
    IReadOnlyList<string> Guardrails)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (OpportunityThemes.Count == 0) errors[nameof(OpportunityThemes)] = ["At least one opportunity theme is required."];
        if (EvidenceSignals.Count == 0) errors[nameof(EvidenceSignals)] = ["At least one evidence signal is required."];
        if (Guardrails.Count == 0) errors[nameof(Guardrails)] = ["At least one guardrail is required."];
        if (OpportunityThemes.Concat(EvidenceSignals).Concat(Guardrails).Any(string.IsNullOrWhiteSpace))
            errors["content"] = ["Knowledge Pack content cannot contain blank values."];
        return errors;
    }
}

[Index(nameof(Key), nameof(Version), IsUnique = true)]
public sealed class KnowledgePack
{
    public Guid Id { get; private set; }
    public required string Key { get; init; }
    public required string Version { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string ContentJson { get; init; }
    public DateTimeOffset PublishedAt { get; init; }

    public KnowledgePackContent Content =>
        JsonSerializer.Deserialize<KnowledgePackContent>(ContentJson) ?? throw new InvalidOperationException("Knowledge Pack content is invalid.");

    public static KnowledgePack Publish(string key, string version, string displayName, string description, KnowledgePackContent content, DateTimeOffset? publishedAt = null)
    {
        if (!KnowledgePackKeys.IsValid(key)) throw new ArgumentException("Knowledge Pack key is invalid.", nameof(key));
        if (!KnowledgePackVersions.IsValid(version)) throw new ArgumentException("Knowledge Pack version must use major.minor format.", nameof(version));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        var errors = content.Validate();
        if (errors.Count > 0) throw new ArgumentException("Knowledge Pack content is invalid.", nameof(content));

        return new KnowledgePack
        {
            Id = Guid.NewGuid(),
            Key = key,
            Version = version,
            DisplayName = displayName.Trim(),
            Description = description.Trim(),
            ContentJson = JsonSerializer.Serialize(content),
            PublishedAt = publishedAt ?? DateTimeOffset.UtcNow
        };
    }
}

[Index(nameof(BusinessId), nameof(PackKey), nameof(PackVersion), IsUnique = true)]
[Index(nameof(BusinessId), nameof(IsActive))]
public sealed class BusinessKnowledgePack
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid KnowledgePackId { get; set; }
    public required string PackKey { get; set; }
    public required string PackVersion { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public Business Business { get; set; } = null!;
    public KnowledgePack KnowledgePack { get; set; } = null!;

    public static BusinessKnowledgePack Assign(Guid businessId, KnowledgePack pack, bool active = true) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        KnowledgePackId = pack.Id,
        PackKey = pack.Key,
        PackVersion = pack.Version,
        IsActive = active,
        AssignedAt = DateTimeOffset.UtcNow,
        KnowledgePack = pack
    };
}

public sealed record KnowledgePackResponse(
    string Key,
    string Version,
    string DisplayName,
    string Description,
    KnowledgePackContent Content,
    DateTimeOffset PublishedAt,
    DateTimeOffset AssignedAt)
{
    public static KnowledgePackResponse From(BusinessKnowledgePack assignment) => new(
        assignment.PackKey,
        assignment.PackVersion,
        assignment.KnowledgePack.DisplayName,
        assignment.KnowledgePack.Description,
        assignment.KnowledgePack.Content,
        assignment.KnowledgePack.PublishedAt,
        assignment.AssignedAt);
}

public static class GenericBusinessKnowledgePack
{
    public const string Version = "1.0";

    public static KnowledgePack Create() => KnowledgePack.Publish(
        KnowledgePackKeys.GenericBusiness,
        Version,
        "Generic Business",
        "Industry-agnostic knowledge for identifying practical business opportunities.",
        new KnowledgePackContent(
            ["revenue", "retention", "efficiency", "customer-experience", "risk-reduction"],
            ["business-profile", "business-goals", "business-context", "owner-confirmed-data"],
            ["require-evidence", "respect-business-boundaries", "avoid-unsupported-claims", "retain-pack-version"]));
}
