using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class MembershipRoles
{
    public const string BusinessOwner = "BusinessOwner";
    public const string PilotOperator = "PilotOperator";
    public const string PlatformAdministrator = "PlatformAdministrator";
}

public static class FieldSources
{
    public const string Owner = "owner";
    public const string Public = "public";
}

public sealed record CreateBusinessRequest(string Name, string Category, string Country, string Timezone, string Currency, string PrimaryLocation, string OperatingStatus)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        static bool Missing(string? value) => string.IsNullOrWhiteSpace(value);
        if (Missing(Name)) errors[nameof(Name)] = ["Business name is required."];
        if (Missing(Category)) errors[nameof(Category)] = ["Category is required."];
        if (Missing(Country)) errors[nameof(Country)] = ["Country is required."];
        if (Missing(Timezone)) errors[nameof(Timezone)] = ["Timezone is required."];
        if (Missing(Currency) || Currency.Length != 3) errors[nameof(Currency)] = ["Currency must be a three-letter ISO code."];
        if (Missing(PrimaryLocation)) errors[nameof(PrimaryLocation)] = ["Primary location is required."];
        if (Missing(OperatingStatus)) errors[nameof(OperatingStatus)] = ["Operating status is required."];
        return errors;
    }
}

public sealed record BusinessResponse(Guid Id, string Name, string Category, string Country, string Timezone, string Currency, string PrimaryLocation, string OperatingStatus)
{
    public static BusinessResponse From(Business business) => new(business.Id, business.Name, business.Category, business.Country, business.Timezone, business.Currency, business.PrimaryLocation, business.OperatingStatus);
}

public sealed record UpsertBusinessProfileRequest(string? Description, string? Address, string? Website, string? Phone, string? Email, string? SocialChannels, string? BusinessHours, string Language, string Source, bool OwnerConfirmed)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(Language)) errors[nameof(Language)] = ["Language is required."];
        if (Source is not FieldSources.Owner and not FieldSources.Public) errors[nameof(Source)] = ["Source must be owner or public."];
        if (Source == FieldSources.Public && !OwnerConfirmed) errors[nameof(OwnerConfirmed)] = ["Publicly sourced profile data must be owner-confirmed."];
        return errors;
    }
}

public sealed record UpsertGoalsRequest(IReadOnlyList<GoalInput> Goals)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (Goals.Count == 0) errors[nameof(Goals)] = ["At least one goal is required."];
        if (Goals.Count > 10) errors[nameof(Goals)] = ["A maximum of ten goals is supported."];
        if (Goals.Any(x => string.IsNullOrWhiteSpace(x.Title))) errors[nameof(Goals)] = ["Every goal requires a title."];
        if (Goals.Select(x => x.Priority).Distinct().Count() != Goals.Count) errors[nameof(Goals)] = ["Goal priorities must be unique."];
        return errors;
    }
}

public sealed record GoalInput(string Type, string Title, int Priority, bool IsCustom);
public sealed record UpsertContextRequest(string Key, string Value, string Source, bool OwnerConfirmed);

public sealed class UserAccount { public Guid Id { get; set; } public required string ProviderSubject { get; set; } public DateTimeOffset CreatedAt { get; set; } }
public sealed class Business
{
    public Guid Id { get; set; } public required string Name { get; set; } public required string Category { get; set; }
    public required string Country { get; set; } public required string Timezone { get; set; } public required string Currency { get; set; }
    public required string PrimaryLocation { get; set; } public required string OperatingStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; } public uint Version { get; set; }
    public static Business Create(CreateBusinessRequest request) => new() { Id = Guid.NewGuid(), Name = request.Name.Trim(), Category = request.Category.Trim(), Country = request.Country.Trim(), Timezone = request.Timezone.Trim(), Currency = request.Currency.Trim().ToUpperInvariant(), PrimaryLocation = request.PrimaryLocation.Trim(), OperatingStatus = request.OperatingStatus.Trim(), CreatedAt = DateTimeOffset.UtcNow };
}
public sealed class BusinessProfile { public Guid BusinessId { get; set; } public string? Description { get; set; } public string? Address { get; set; } public string? Website { get; set; } public string? Phone { get; set; } public string? Email { get; set; } public string? SocialChannels { get; set; } public string? BusinessHours { get; set; } public required string Language { get; set; } public required string Source { get; set; } public bool OwnerConfirmed { get; set; } public DateTimeOffset UpdatedAt { get; set; } }
public sealed class BusinessGoal { public Guid Id { get; set; } public Guid BusinessId { get; set; } public required string Type { get; set; } public required string Title { get; set; } public int Priority { get; set; } public bool IsCustom { get; set; } public DateTimeOffset UpdatedAt { get; set; } }
public sealed class BusinessContextEntry { public Guid Id { get; set; } public Guid BusinessId { get; set; } public required string Key { get; set; } public required string Value { get; set; } public required string Source { get; set; } public bool OwnerConfirmed { get; set; } public DateTimeOffset UpdatedAt { get; set; } }
public sealed class BusinessMembership { public Guid Id { get; set; } public Guid BusinessId { get; set; } public Guid UserAccountId { get; set; } public required string Role { get; set; } public DateTimeOffset CreatedAt { get; set; } public UserAccount UserAccount { get; set; } = null!; }
public sealed class AuditRecord
{
    public Guid Id { get; set; } public Guid UserAccountId { get; set; } public Guid? BusinessId { get; set; }
    public required string Action { get; set; } public DateTimeOffset OccurredAt { get; set; }
    public static AuditRecord Create(Guid accountId, Guid? businessId, string action) => new() { Id = Guid.NewGuid(), UserAccountId = accountId, BusinessId = businessId, Action = action, OccurredAt = DateTimeOffset.UtcNow };
}

public sealed class AtlasDbContext(DbContextOptions<AtlasDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>(); public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>(); public DbSet<BusinessGoal> BusinessGoals => Set<BusinessGoal>();
    public DbSet<BusinessContextEntry> BusinessContextEntries => Set<BusinessContextEntry>(); public DbSet<BusinessMembership> BusinessMemberships => Set<BusinessMembership>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>(); public DbSet<KnowledgePack> KnowledgePacks => Set<KnowledgePack>();
    public DbSet<KnowledgePackVersion> KnowledgePackVersions => Set<KnowledgePackVersion>(); public DbSet<KnowledgeSection> KnowledgeSections => Set<KnowledgeSection>();
    public DbSet<BusinessKnowledgeAssignment> BusinessKnowledgeAssignments => Set<BusinessKnowledgeAssignment>();
    public DbSet<ExecutionKit> ExecutionKits => Set<ExecutionKit>(); public DbSet<ExecutionAsset> ExecutionAssets => Set<ExecutionAsset>();
    public DbSet<ActionDecisionRecord> ActionDecisionRecords => Set<ActionDecisionRecord>();
    public DbSet<Outcome> Outcomes => Set<Outcome>(); public DbSet<BusinessMemoryItem> BusinessMemoryItems => Set<BusinessMemoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>().HasIndex(x => x.ProviderSubject).IsUnique();
        modelBuilder.Entity<Business>().Property(x => x.Version).IsRowVersion();
        modelBuilder.Entity<BusinessProfile>().HasKey(x => x.BusinessId);
        modelBuilder.Entity<BusinessGoal>().HasIndex(x => new { x.BusinessId, x.Priority }).IsUnique();
        modelBuilder.Entity<BusinessContextEntry>().HasIndex(x => new { x.BusinessId, x.Key }).IsUnique();
        modelBuilder.Entity<BusinessMembership>().HasIndex(x => new { x.BusinessId, x.UserAccountId, x.Role }).IsUnique();
        modelBuilder.Entity<BusinessMembership>().HasOne(x => x.UserAccount).WithMany().HasForeignKey(x => x.UserAccountId);
        modelBuilder.Entity<AuditRecord>().HasIndex(x => new { x.BusinessId, x.OccurredAt });

        modelBuilder.Entity<KnowledgePack>().Property(x => x.Version).IsRowVersion();
        modelBuilder.Entity<KnowledgePackVersion>().Property(x => x.ConcurrencyVersion).IsRowVersion();
        modelBuilder.Entity<KnowledgeSection>().Property(x => x.ConcurrencyVersion).IsRowVersion();
        modelBuilder.Entity<BusinessKnowledgeAssignment>().Property(x => x.ConcurrencyVersion).IsRowVersion();
        modelBuilder.Entity<KnowledgePackVersion>().HasOne(x => x.KnowledgePack).WithMany(x => x.Versions).HasForeignKey(x => x.KnowledgePackId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<KnowledgeSection>().HasOne(x => x.KnowledgePackVersion).WithMany(x => x.Sections).HasForeignKey(x => x.KnowledgePackVersionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BusinessKnowledgeAssignment>().HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BusinessKnowledgeAssignment>().HasOne(x => x.KnowledgePack).WithMany().HasForeignKey(x => x.KnowledgePackId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BusinessKnowledgeAssignment>().HasOne(x => x.KnowledgePackVersion).WithMany().HasForeignKey(x => x.KnowledgePackVersionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BusinessKnowledgeAssignment>().HasIndex(x => x.BusinessId).IsUnique().HasFilter("\"IsCurrent\" = TRUE");

        modelBuilder.Entity<ExecutionKit>().Property(x => x.ConcurrencyVersion).IsRowVersion();
        modelBuilder.Entity<ExecutionAsset>().Property(x => x.ConcurrencyVersion).IsRowVersion();
        modelBuilder.Entity<ExecutionKit>().HasIndex(x => new { x.BusinessId, x.OpportunityId }).IsUnique();
        modelBuilder.Entity<ExecutionAsset>().HasOne(x => x.ExecutionKit).WithMany(x => x.Assets).HasForeignKey(x => x.ExecutionKitId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ExecutionAsset>().HasIndex(x => new { x.ExecutionKitId, x.Type, x.Title }).IsUnique();

        modelBuilder.Entity<ActionDecisionRecord>().HasIndex(x => new { x.BusinessId, x.OpportunityId, x.DecidedAt });
        modelBuilder.Entity<ActionDecisionRecord>().HasIndex(x => new { x.OpportunityId, x.DecidedAt });

        modelBuilder.Entity<Outcome>().Property(x => x.ConcurrencyVersion).IsRowVersion();
        modelBuilder.Entity<Outcome>().Property(x => x.ResultSummary).HasMaxLength(1000);
        modelBuilder.Entity<Outcome>().Property(x => x.OwnerNotes).HasMaxLength(2000);
        modelBuilder.Entity<Outcome>().HasIndex(x => new { x.BusinessId, x.OpportunityId }).IsUnique();
        modelBuilder.Entity<Outcome>().HasIndex(x => new { x.BusinessId, x.FollowUpAt });

        modelBuilder.Entity<BusinessMemoryItem>().Property(x => x.ConcurrencyVersion).IsRowVersion();
        modelBuilder.Entity<BusinessMemoryItem>().Property(x => x.StableKey).HasMaxLength(200);
        modelBuilder.Entity<BusinessMemoryItem>().Property(x => x.Value).HasMaxLength(2000);
        modelBuilder.Entity<BusinessMemoryItem>().HasIndex(x => new { x.BusinessId, x.StableKey }).IsUnique();
        modelBuilder.Entity<BusinessMemoryItem>().HasIndex(x => new { x.BusinessId, x.Category, x.UpdatedAt });
    }
}