using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

[Table("BusinessDiscoveryMediaReferences")]
[Index(nameof(SnapshotId))]
public sealed class BusinessDiscoveryMediaReference
{
    public Guid Id { get; set; }
    public Guid SnapshotId { get; set; }
    public int SourceOrder { get; set; }
    [MaxLength(40)] public required string Kind { get; set; }
    [MaxLength(2000)] public required string RemoteUrl { get; set; }
    [MaxLength(80)] public required string Source { get; set; }
    [MaxLength(2000)] public required string SourceUrl { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    [MaxLength(20)] public required string Confidence { get; set; }
    [MaxLength(40)] public required string EvidenceClass { get; set; }
    public bool OwnerConfirmed { get; set; }
    [MaxLength(500)] public string? AltText { get; set; }
    public BusinessDiscoverySnapshot Snapshot { get; set; } = null!;
}

[Table("BusinessDiscoveryOfferings")]
[Index(nameof(SnapshotId))]
[Index(nameof(SnapshotId), nameof(Kind), nameof(Name))]
public sealed class BusinessDiscoveryOffering
{
    public Guid Id { get; set; }
    public Guid SnapshotId { get; set; }
    public int SourceOrder { get; set; }
    [MaxLength(40)] public required string Kind { get; set; }
    [MaxLength(240)] public string? Section { get; set; }
    [MaxLength(240)] public required string Name { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    [Precision(18, 2)] public decimal? Price { get; set; }
    [MaxLength(3)] public string? Currency { get; set; }
    [MaxLength(80)] public required string Source { get; set; }
    [MaxLength(2000)] public required string SourceUrl { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    [MaxLength(20)] public required string Confidence { get; set; }
    [MaxLength(40)] public required string EvidenceClass { get; set; }
    public bool OwnerConfirmed { get; set; }
    public BusinessDiscoverySnapshot Snapshot { get; set; } = null!;
}

[Table("BusinessMediaReferences")]
[Index(nameof(BusinessId))]
[Index(nameof(SourceSnapshotId))]
public sealed class BusinessMediaReference
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? SourceSnapshotId { get; set; }
    public int SourceOrder { get; set; }
    [MaxLength(40)] public required string Kind { get; set; }
    [MaxLength(2000)] public required string RemoteUrl { get; set; }
    [MaxLength(80)] public required string Source { get; set; }
    [MaxLength(2000)] public required string SourceUrl { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    [MaxLength(20)] public required string Confidence { get; set; }
    [MaxLength(40)] public required string EvidenceClass { get; set; }
    public bool OwnerConfirmed { get; set; }
    [MaxLength(500)] public string? AltText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Business Business { get; set; } = null!;
    public BusinessDiscoverySnapshot? SourceSnapshot { get; set; }
}

[Table("BusinessOfferings")]
[Index(nameof(BusinessId))]
[Index(nameof(SourceSnapshotId))]
[Index(nameof(BusinessId), nameof(Kind), nameof(Name))]
public sealed class BusinessOffering
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? SourceSnapshotId { get; set; }
    public int SourceOrder { get; set; }
    [MaxLength(40)] public required string Kind { get; set; }
    [MaxLength(240)] public string? Section { get; set; }
    [MaxLength(240)] public required string Name { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    [Precision(18, 2)] public decimal? Price { get; set; }
    [MaxLength(3)] public string? Currency { get; set; }
    [MaxLength(80)] public required string Source { get; set; }
    [MaxLength(2000)] public required string SourceUrl { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    [MaxLength(20)] public required string Confidence { get; set; }
    [MaxLength(40)] public required string EvidenceClass { get; set; }
    public bool OwnerConfirmed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Business Business { get; set; } = null!;
    public BusinessDiscoverySnapshot? SourceSnapshot { get; set; }
}

public static class BusinessMediaMenuPersistence
{
    public static List<BusinessDiscoveryMediaReference> DiscoveryMedia(
        BusinessDiscoverySnapshot snapshot,
        IReadOnlyList<PublicBusinessMedia> media) =>
        media.Select(item => new BusinessDiscoveryMediaReference
        {
            Id = Guid.NewGuid(),
            SnapshotId = snapshot.Id,
            Snapshot = snapshot,
            SourceOrder = item.SourceOrder,
            Kind = item.Kind,
            RemoteUrl = item.RemoteUrl,
            Source = item.Source,
            SourceUrl = item.SourceUrl,
            ObservedAt = item.ObservedAt,
            Confidence = item.Confidence,
            EvidenceClass = item.EvidenceClass,
            OwnerConfirmed = false,
            AltText = item.AltText
        }).ToList();

    public static List<BusinessDiscoveryOffering> DiscoveryOfferings(
        BusinessDiscoverySnapshot snapshot,
        IReadOnlyList<PublicBusinessOffering> offerings) =>
        offerings.Select(item => new BusinessDiscoveryOffering
        {
            Id = Guid.NewGuid(),
            SnapshotId = snapshot.Id,
            Snapshot = snapshot,
            SourceOrder = item.SourceOrder,
            Kind = item.Kind,
            Section = item.Section,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            Currency = item.Currency,
            Source = item.Source,
            SourceUrl = item.SourceUrl,
            ObservedAt = item.ObservedAt,
            Confidence = item.Confidence,
            EvidenceClass = item.EvidenceClass,
            OwnerConfirmed = false
        }).ToList();

    public static List<BusinessMediaReference> BusinessMedia(
        BusinessDiscoverySnapshot snapshot,
        Business business,
        DateTimeOffset createdAt) =>
        snapshot.Media.Select(item => new BusinessMediaReference
        {
            BusinessId = business.Id,
            Business = business,
            SourceSnapshotId = snapshot.Id,
            SourceSnapshot = snapshot,
            SourceOrder = item.SourceOrder,
            Kind = item.Kind,
            RemoteUrl = item.RemoteUrl,
            Source = item.Source,
            SourceUrl = item.SourceUrl,
            ObservedAt = item.ObservedAt,
            Confidence = item.Confidence,
            EvidenceClass = item.EvidenceClass,
            OwnerConfirmed = false,
            AltText = item.AltText,
            CreatedAt = createdAt
        }).ToList();

    public static List<BusinessOffering> BusinessOfferings(
        BusinessDiscoverySnapshot snapshot,
        Business business,
        DateTimeOffset createdAt) =>
        snapshot.Offerings.Select(item => new BusinessOffering
        {
            BusinessId = business.Id,
            Business = business,
            SourceSnapshotId = snapshot.Id,
            SourceSnapshot = snapshot,
            SourceOrder = item.SourceOrder,
            Kind = item.Kind,
            Section = item.Section,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            Currency = item.Currency,
            Source = item.Source,
            SourceUrl = item.SourceUrl,
            ObservedAt = item.ObservedAt,
            Confidence = item.Confidence,
            EvidenceClass = item.EvidenceClass,
            OwnerConfirmed = false,
            CreatedAt = createdAt
        }).ToList();
}
