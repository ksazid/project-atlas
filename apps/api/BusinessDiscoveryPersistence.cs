using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Atlas.Api;

public sealed class BusinessDiscoverySnapshot
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public required string Provider { get; set; }
    public required string SourceUrl { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public Guid? BusinessId { get; set; }
    public ICollection<BusinessDiscoveryFact> Facts { get; set; } = [];

    public static BusinessDiscoverySnapshot Create(Guid accountId, PublicBusinessSnapshot snapshot) => new()
    {
        Id = Guid.NewGuid(),
        UserAccountId = accountId,
        Provider = snapshot.Provider,
        SourceUrl = snapshot.SourceUrl,
        ObservedAt = snapshot.ObservedAt,
        CreatedAt = DateTimeOffset.UtcNow,
        Facts = snapshot.Facts.Select(fact => new BusinessDiscoveryFact
        {
            Id = Guid.NewGuid(),
            Key = fact.Key,
            Value = fact.Value,
            Source = fact.Source,
            SourceUrl = fact.SourceUrl,
            ObservedAt = fact.ObservedAt,
            Confidence = fact.Confidence,
            EvidenceClass = fact.EvidenceClass,
            OwnerConfirmed = fact.OwnerConfirmed
        }).ToList()
    };

    public bool CanBeConsumedBy(Guid accountId) => UserAccountId == accountId && ConsumedAt is null && BusinessId is null;

    public void MarkConsumed(Guid businessId, DateTimeOffset consumedAt)
    {
        if (ConsumedAt is not null || BusinessId is not null) throw new InvalidOperationException("Discovery snapshot has already been consumed.");
        BusinessId = businessId;
        ConsumedAt = consumedAt;
    }
}

public sealed class BusinessDiscoveryFact
{
    public Guid Id { get; set; }
    public Guid SnapshotId { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required string Source { get; set; }
    public required string SourceUrl { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public required string Confidence { get; set; }
    public required string EvidenceClass { get; set; }
    public bool OwnerConfirmed { get; set; }
    public BusinessDiscoverySnapshot Snapshot { get; set; } = null!;
}

public sealed class BusinessProfileField
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required string Source { get; set; }
    public string? SourceUrl { get; set; }
    public DateTimeOffset? ObservedAt { get; set; }
    public string? Confidence { get; set; }
    public required string EvidenceClass { get; set; }
    public bool OwnerConfirmed { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public static class BusinessDiscoveryProvenance
{
    public static BusinessProfileField Resolve(Guid businessId, string key, string authoritativeValue, BusinessDiscoveryFact? observed, DateTimeOffset updatedAt)
    {
        var value = authoritativeValue.Trim();
        var acceptedPublic = observed is not null && string.Equals(observed.Value.Trim(), value, StringComparison.Ordinal);
        return new BusinessProfileField
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Key = key,
            Value = value,
            Source = acceptedPublic ? FieldSources.Public : FieldSources.Owner,
            SourceUrl = acceptedPublic ? observed!.SourceUrl : null,
            ObservedAt = acceptedPublic ? observed!.ObservedAt : null,
            Confidence = acceptedPublic ? observed!.Confidence : null,
            EvidenceClass = acceptedPublic ? "public-observed" : "owner-reported",
            OwnerConfirmed = true,
            UpdatedAt = updatedAt
        };
    }
}

public sealed record CreateBusinessFromDiscoveryRequest(
    Guid SnapshotId,
    string Name,
    string Category,
    string? Subcategory,
    string Country,
    string Timezone,
    string Currency,
    string PrimaryLocation,
    string OperatingStatus,
    string? Description,
    string? Website,
    string? Phone,
    string? BusinessHours,
    string Language,
    bool OwnerConfirmed)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new CreateBusinessRequest(Name, Category, Country, Timezone, Currency, PrimaryLocation, OperatingStatus).Validate();
        if (!BusinessCategoryTaxonomy.IsKnownCategory(Category)) errors[nameof(Category)] = ["Choose a supported Atlas business category."];
        if (!BusinessCategoryTaxonomy.IsKnownSubcategory(Category, Subcategory)) errors[nameof(Subcategory)] = ["Choose a subcategory that belongs to the selected category."];
        if (string.IsNullOrWhiteSpace(Language)) errors[nameof(Language)] = ["Language is required."];
        if (!OwnerConfirmed) errors[nameof(OwnerConfirmed)] = ["Review and confirm the discovered business details before continuing."];
        return errors;
    }
}

public static class BusinessDiscoveryBusinessCreator
{
    public static async Task<BusinessResponse> CreateAsync(AtlasDbContext db, string subject, CreateBusinessFromDiscoveryRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subject)) throw new BusinessDiscoveryException("business_owner_missing", "Sign in again before finishing business setup.");
        var errors = request.Validate();
        if (errors.Count > 0) throw new BusinessDiscoveryValidationException(errors);

        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.ProviderSubject == subject, ct)
            ?? throw new BusinessDiscoveryException("business_discovery_not_found", "Run business discovery again before continuing.");

        var snapshot = await db.BusinessDiscoverySnapshots.Include(x => x.Facts).SingleOrDefaultAsync(x => x.Id == request.SnapshotId, ct)
            ?? throw new BusinessDiscoveryException("business_discovery_not_found", "Run business discovery again before continuing.");

        if (snapshot.UserAccountId != account.Id)
            throw new BusinessDiscoveryException("business_discovery_not_found", "Run business discovery again before continuing.");
        if (snapshot.ConsumedAt is not null || snapshot.BusinessId is not null)
            throw new BusinessDiscoveryException("business_discovery_consumed", "This discovery has already been used. Start a new business setup if needed.");
        if (await db.BusinessMemberships.AnyAsync(x => x.UserAccountId == account.Id && x.Role == MembershipRoles.BusinessOwner, ct))
            throw new BusinessDiscoveryException("initial_business_exists", "The account already owns a Business.");

        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational()) transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var businessRequest = new CreateBusinessRequest(request.Name, request.Category, request.Country, request.Timezone, request.Currency, request.PrimaryLocation, request.OperatingStatus);
            var business = Business.Create(businessRequest);
            var now = DateTimeOffset.UtcNow;

            var genericVersion = await db.KnowledgePackVersions
                .Include(x => x.KnowledgePack)
                .Include(x => x.Sections)
                .SingleOrDefaultAsync(x => x.KnowledgePack.Key == KnowledgePackKeys.GenericBusiness &&
                    x.VersionNumber == GenericBusinessKnowledgePack.InitialVersion && x.Status == KnowledgePackStatuses.Published, ct);

            KnowledgePack genericPack;
            if (genericVersion is null)
            {
                (genericPack, genericVersion) = GenericBusinessKnowledgePack.Create(account.Id);
                db.KnowledgePacks.Add(genericPack);
            }
            else genericPack = genericVersion.KnowledgePack;

            db.Businesses.Add(business);
            db.BusinessMemberships.Add(new BusinessMembership
            {
                Id = Guid.NewGuid(), BusinessId = business.Id, UserAccountId = account.Id,
                UserAccount = account, Role = MembershipRoles.BusinessOwner, CreatedAt = now
            });
            db.BusinessKnowledgeAssignments.Add(BusinessKnowledgeAssignment.Assign(business.Id, genericPack, genericVersion, account.Id));

            var profile = new BusinessProfile
            {
                BusinessId = business.Id,
                Description = Clean(request.Description),
                Address = business.PrimaryLocation,
                Website = Clean(request.Website),
                Phone = Clean(request.Phone),
                BusinessHours = Clean(request.BusinessHours),
                Language = request.Language.Trim(),
                Source = FieldSources.Owner,
                OwnerConfirmed = true,
                UpdatedAt = now
            };
            db.BusinessProfiles.Add(profile);

            var observed = snapshot.Facts.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
            AddField("name", business.Name);
            AddField("category", business.Category);
            if (!string.IsNullOrWhiteSpace(request.Subcategory)) AddField("subcategory", request.Subcategory!);
            AddField("country", business.Country);
            AddField("timezone", business.Timezone);
            AddField("currency", business.Currency);
            AddField("primaryLocation", business.PrimaryLocation);
            AddField("operatingStatus", business.OperatingStatus);
            if (!string.IsNullOrWhiteSpace(profile.Description)) AddField("description", profile.Description!);
            if (!string.IsNullOrWhiteSpace(profile.Website)) AddField("website", profile.Website!);
            if (!string.IsNullOrWhiteSpace(profile.Phone)) AddField("phone", profile.Phone!);
            if (!string.IsNullOrWhiteSpace(profile.BusinessHours)) AddField("openingHours", profile.BusinessHours!);
            AddField("language", profile.Language);

            snapshot.MarkConsumed(business.Id, now);
            db.AuditRecords.Add(AuditRecord.Create(account.Id, business.Id, "business.created"));
            db.AuditRecords.Add(AuditRecord.Create(account.Id, business.Id, "business.discovery.confirmed"));
            db.AuditRecords.Add(AuditRecord.Create(account.Id, business.Id, $"knowledge-pack.assigned:{genericPack.Key}:{genericVersion.VersionNumber}"));

            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return BusinessResponse.From(business);

            void AddField(string key, string value)
            {
                observed.TryGetValue(key, out var fact);
                db.BusinessProfileFields.Add(BusinessDiscoveryProvenance.Resolve(business.Id, key, value, fact, now));
            }
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class BusinessDiscoveryValidationException(Dictionary<string, string[]> errors) : Exception("Business discovery confirmation is invalid.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}
