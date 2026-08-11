using System.ComponentModel.DataAnnotations;
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
    public ICollection<BusinessDiscoverySource> Sources { get; set; } = [];
    public ICollection<BusinessDiscoveryEvidence> Evidence { get; set; } = [];
    public ICollection<BusinessDiscoveryMediaReference> Media { get; set; } = [];
    public ICollection<BusinessDiscoveryOffering> Offerings { get; set; } = [];
    public ICollection<BusinessMediaReference> MaterializedMedia { get; set; } = [];
    public ICollection<BusinessOffering> MaterializedOfferings { get; set; } = [];

    public static BusinessDiscoverySnapshot Create(Guid accountId, PublicBusinessSnapshot snapshot)
    {
        var normalized = NormalizeSnapshot(snapshot);
        var entity = CreateBase(accountId, normalized);
        var source = new BusinessDiscoverySource
        {
            Id = Guid.NewGuid(),
            SnapshotId = entity.Id,
            Snapshot = entity,
            Order = 0,
            IsPrimary = true,
            Provider = normalized.Provider,
            CanonicalUrl = normalized.SourceUrl,
            ObservedAt = normalized.ObservedAt,
            Status = "success",
            AssociationStatus = "anchor"
        };
        entity.Sources.Add(source);
        entity.Facts = SelectedFacts(entity, normalized.Facts);
        entity.Media = BusinessMediaMenuPersistence.DiscoveryMedia(entity, normalized.Media);
        entity.Offerings = BusinessMediaMenuPersistence.DiscoveryOfferings(entity, normalized.Offerings);
        entity.Evidence = normalized.Facts.Select(fact => new BusinessDiscoveryEvidence
        {
            Id = Guid.NewGuid(),
            SnapshotId = entity.Id,
            Snapshot = entity,
            SourceId = source.Id,
            Source = source,
            SourceOrder = 0,
            Provider = normalized.Provider,
            CanonicalUrl = normalized.SourceUrl,
            Key = fact.Key,
            Value = fact.Value,
            ObservedAt = fact.ObservedAt,
            Confidence = fact.Confidence,
            EvidenceClass = fact.EvidenceClass,
            ReconciliationState = "selected",
            AssociationStatus = "anchor"
        }).ToList();
        source.Evidence = entity.Evidence.ToList();
        return entity;
    }

    public static BusinessDiscoverySnapshot Create(Guid accountId, BusinessDiscoveryReconciliationResult reconciliation)
    {
        ArgumentNullException.ThrowIfNull(reconciliation);
        var normalized = NormalizeSnapshot(reconciliation.Snapshot);
        var entity = CreateBase(accountId, normalized);
        entity.Facts = SelectedFacts(entity, normalized.Facts);
        entity.Media = BusinessMediaMenuPersistence.DiscoveryMedia(entity, normalized.Media);
        entity.Offerings = BusinessMediaMenuPersistence.DiscoveryOfferings(entity, normalized.Offerings);

        var sources = reconciliation.SourceResults
            .OrderBy(x => x.Order)
            .Select(result => new BusinessDiscoverySource
            {
                Id = Guid.NewGuid(),
                SnapshotId = entity.Id,
                Snapshot = entity,
                Order = result.Order,
                IsPrimary = result.IsPrimary,
                Provider = result.Provider,
                CanonicalUrl = result.CanonicalUrl,
                ObservedAt = result.ObservedAt,
                Status = result.Status,
                WarningCode = result.WarningCode,
                AssociationStatus = result.AssociationStatus
            })
            .ToList();
        entity.Sources = sources;
        var byOrder = sources.ToDictionary(x => x.Order);

        entity.Evidence = reconciliation.Evidence
            .Where(candidate => byOrder.ContainsKey(candidate.SourceOrder))
            .Select(candidate =>
            {
                var source = byOrder[candidate.SourceOrder];
                return new BusinessDiscoveryEvidence
                {
                    Id = Guid.NewGuid(),
                    SnapshotId = entity.Id,
                    Snapshot = entity,
                    SourceId = source.Id,
                    Source = source,
                    SourceOrder = candidate.SourceOrder,
                    Provider = candidate.Provider,
                    CanonicalUrl = candidate.CanonicalUrl,
                    Key = candidate.Key,
                    Value = candidate.Value,
                    ObservedAt = candidate.ObservedAt,
                    Confidence = candidate.Confidence,
                    EvidenceClass = candidate.EvidenceClass,
                    ReconciliationState = candidate.ReconciliationState,
                    AssociationStatus = candidate.AssociationStatus
                };
            })
            .ToList();

        foreach (var source in sources)
            source.Evidence = entity.Evidence.Where(x => x.SourceId == source.Id).ToList();

        return entity;
    }

    public bool CanBeConsumedBy(Guid accountId) => UserAccountId == accountId && ConsumedAt is null && BusinessId is null;

    public void MarkConsumed(Guid businessId, DateTimeOffset consumedAt)
    {
        if (ConsumedAt is not null || BusinessId is not null) throw new InvalidOperationException("Discovery snapshot has already been consumed.");
        BusinessId = businessId;
        ConsumedAt = consumedAt;
    }

    private static BusinessDiscoverySnapshot CreateBase(Guid accountId, PublicBusinessSnapshot snapshot) => new()
    {
        Id = Guid.NewGuid(),
        UserAccountId = accountId,
        Provider = snapshot.Provider,
        SourceUrl = snapshot.SourceUrl,
        ObservedAt = snapshot.ObservedAt,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static List<BusinessDiscoveryFact> SelectedFacts(BusinessDiscoverySnapshot snapshot, IReadOnlyList<PublicBusinessFact> facts) =>
        facts.Select(fact => new BusinessDiscoveryFact
        {
            Id = Guid.NewGuid(),
            SnapshotId = snapshot.Id,
            Snapshot = snapshot,
            Key = fact.Key,
            Value = fact.Value,
            Source = fact.Source,
            SourceUrl = fact.SourceUrl,
            ObservedAt = fact.ObservedAt,
            Confidence = fact.Confidence,
            EvidenceClass = fact.EvidenceClass,
            OwnerConfirmed = fact.OwnerConfirmed
        }).ToList();

    private static PublicBusinessSnapshot NormalizeSnapshot(PublicBusinessSnapshot snapshot)
    {
        var normalizedFacts = snapshot.Facts.ToList();
        if (snapshot.Provider is "bolt-food" or "wolt" && Uri.TryCreate(snapshot.SourceUrl, UriKind.Absolute, out var sourceUri))
        {
            var nameIndex = normalizedFacts.FindIndex(x => x.Key.Equals("name", StringComparison.OrdinalIgnoreCase));
            var observedName = nameIndex >= 0 ? normalizedFacts[nameIndex].Value : null;
            var resolvedName = MarketplaceBusinessIdentity.ResolveName(snapshot.Provider, sourceUri, observedName);
            if (!string.IsNullOrWhiteSpace(resolvedName.Value))
            {
                if (nameIndex >= 0)
                {
                    var existing = normalizedFacts[nameIndex];
                    normalizedFacts[nameIndex] = existing with { Value = resolvedName.Value, Confidence = resolvedName.Confidence };
                }
                else
                {
                    normalizedFacts.Add(new PublicBusinessFact(
                        "name", resolvedName.Value, snapshot.Provider, snapshot.SourceUrl, snapshot.ObservedAt,
                        resolvedName.Confidence, "public-observed", false));
                }
            }
        }

        return snapshot with { Facts = normalizedFacts };
    }
}

[Index(nameof(SnapshotId), nameof(Order), IsUnique = true)]
public sealed class BusinessDiscoverySource
{
    public Guid Id { get; set; }
    public Guid SnapshotId { get; set; }
    public int Order { get; set; }
    public bool IsPrimary { get; set; }
    [MaxLength(80)] public required string Provider { get; set; }
    [MaxLength(2000)] public required string CanonicalUrl { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    [MaxLength(40)] public required string Status { get; set; }
    [MaxLength(120)] public string? WarningCode { get; set; }
    [MaxLength(40)] public required string AssociationStatus { get; set; }
    public BusinessDiscoverySnapshot Snapshot { get; set; } = null!;
    public ICollection<BusinessDiscoveryEvidence> Evidence { get; set; } = [];
}

[Index(nameof(SnapshotId), nameof(Key))]
[Index(nameof(SourceId))]
public sealed class BusinessDiscoveryEvidence
{
    public Guid Id { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid SourceId { get; set; }
    public int SourceOrder { get; set; }
    [MaxLength(80)] public required string Provider { get; set; }
    [MaxLength(2000)] public required string CanonicalUrl { get; set; }
    [MaxLength(80)] public required string Key { get; set; }
    [MaxLength(4000)] public required string Value { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    [MaxLength(20)] public required string Confidence { get; set; }
    [MaxLength(40)] public required string EvidenceClass { get; set; }
    [MaxLength(40)] public required string ReconciliationState { get; set; }
    [MaxLength(40)] public required string AssociationStatus { get; set; }
    public BusinessDiscoverySnapshot Snapshot { get; set; } = null!;
    public BusinessDiscoverySource Source { get; set; } = null!;
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
    public const int MaxValueCharacters = 4000;

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

        BusinessMarketMetadata? market = null;
        if (!string.IsNullOrWhiteSpace(Country) && !string.IsNullOrWhiteSpace(Timezone))
        {
            try
            {
                market = BusinessMarketMetadata.Resolve(Country, Timezone);
            }
            catch (ArgumentException ex) when (ex.ParamName == "countryCode")
            {
                errors[nameof(Country)] = ["Choose a resolved business location so Atlas can set the country automatically."];
            }
            catch (ArgumentException ex) when (ex.ParamName == "timezone")
            {
                errors[nameof(Timezone)] = ["Choose a resolved business location so Atlas can set the timezone automatically."];
            }
        }
        if (market is not null && !string.Equals(Currency.Trim(), market.Currency, StringComparison.OrdinalIgnoreCase))
            errors[nameof(Currency)] = [$"Currency must match the selected business location ({market.Currency})."];

        CheckLength(nameof(Name), Name);
        CheckLength(nameof(Category), Category);
        CheckLength(nameof(Subcategory), Subcategory);
        CheckLength(nameof(Country), Country);
        CheckLength(nameof(Timezone), Timezone);
        CheckLength(nameof(Currency), Currency);
        CheckLength(nameof(PrimaryLocation), PrimaryLocation);
        CheckLength(nameof(OperatingStatus), OperatingStatus);
        CheckLength(nameof(Description), Description);
        CheckLength(nameof(Website), Website);
        CheckLength(nameof(Phone), Phone);
        CheckLength(nameof(BusinessHours), BusinessHours);
        CheckLength(nameof(Language), Language);
        return errors;

        void CheckLength(string field, string? value)
        {
            if (value?.Length > BusinessDiscoveryProvenance.MaxValueCharacters)
                errors[field] = [$"Keep this value to {BusinessDiscoveryProvenance.MaxValueCharacters} characters or fewer."];
        }
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

        var snapshot = await db.BusinessDiscoverySnapshots
            .Include(x => x.Facts)
            .Include(x => x.Media)
            .Include(x => x.Offerings)
            .SingleOrDefaultAsync(x => x.Id == request.SnapshotId, ct)
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

            foreach (var media in BusinessMediaMenuPersistence.BusinessMedia(snapshot, business, now))
                snapshot.MaterializedMedia.Add(media);
            foreach (var offering in BusinessMediaMenuPersistence.BusinessOfferings(snapshot, business, now))
                snapshot.MaterializedOfferings.Add(offering);

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
