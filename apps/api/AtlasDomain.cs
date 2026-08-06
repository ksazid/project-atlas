using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class MembershipRoles
{
    public const string BusinessOwner = "BusinessOwner";
    public const string PilotOperator = "PilotOperator";
    public const string PlatformAdministrator = "PlatformAdministrator";
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

public sealed class UserAccount
{
    public Guid Id { get; set; }
    public required string ProviderSubject { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Business
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string Country { get; set; }
    public required string Timezone { get; set; }
    public required string Currency { get; set; }
    public required string PrimaryLocation { get; set; }
    public required string OperatingStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public uint Version { get; set; }

    public static Business Create(CreateBusinessRequest request) => new()
    {
        Id = Guid.NewGuid(), Name = request.Name.Trim(), Category = request.Category.Trim(), Country = request.Country.Trim(),
        Timezone = request.Timezone.Trim(), Currency = request.Currency.Trim().ToUpperInvariant(), PrimaryLocation = request.PrimaryLocation.Trim(),
        OperatingStatus = request.OperatingStatus.Trim(), CreatedAt = DateTimeOffset.UtcNow
    };
}

public sealed class BusinessMembership
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid UserAccountId { get; set; }
    public required string Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public UserAccount UserAccount { get; set; } = null!;
}

public sealed class AuditRecord
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public Guid? BusinessId { get; set; }
    public required string Action { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public static AuditRecord Create(Guid accountId, Guid? businessId, string action) => new() { Id = Guid.NewGuid(), UserAccountId = accountId, BusinessId = businessId, Action = action, OccurredAt = DateTimeOffset.UtcNow };
}

public sealed class AtlasDbContext(DbContextOptions<AtlasDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessMembership> BusinessMemberships => Set<BusinessMembership>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>().HasIndex(x => x.ProviderSubject).IsUnique();
        modelBuilder.Entity<Business>().Property(x => x.Version).IsRowVersion();
        modelBuilder.Entity<BusinessMembership>().HasIndex(x => new { x.BusinessId, x.UserAccountId, x.Role }).IsUnique();
        modelBuilder.Entity<BusinessMembership>().HasOne(x => x.UserAccount).WithMany().HasForeignKey(x => x.UserAccountId);
        modelBuilder.Entity<AuditRecord>().HasIndex(x => new { x.BusinessId, x.OccurredAt });
    }
}
