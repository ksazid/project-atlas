namespace Atlas.Api;

public sealed class OperationalConnector
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string SourceKind { get; set; }
    public required string FolderId { get; set; }
    public required string FolderName { get; set; }
    public required string Status { get; set; }
    public required string Schedule { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
    public string? ErrorCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class OperationalFileCheckpoint
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid ConnectorId { get; set; }
    public required string ProviderFileId { get; set; }
    public required string FileName { get; set; }
    public required string MimeType { get; set; }
    public long Size { get; set; }
    public DateTimeOffset ProviderModifiedAt { get; set; }
    public required string ContentFingerprint { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}

public sealed class OperationalImport
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? ConnectorId { get; set; }
    public Guid? FileCheckpointId { get; set; }
    public required string SourceKind { get; set; }
    public required string ImportFingerprint { get; set; }
    public required string Status { get; set; }
    public int AcceptedRows { get; set; }
    public int IgnoredColumns { get; set; }
    public DateOnly? EarliestBusinessDate { get; set; }
    public DateOnly? LatestBusinessDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}

public sealed class BusinessSignal
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OperationalImportId { get; set; }
    public required string Identity { get; set; }
    public required string MetricKey { get; set; }
    public decimal Value { get; set; }
    public required string Unit { get; set; }
    public string? Currency { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string? DimensionsJson { get; set; }
    public required string SourceKind { get; set; }
    public required string SourceReference { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public required string Confidence { get; set; }
}

public sealed class BusinessChange
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string Identity { get; set; }
    public required string MetricKey { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ComparisonValue { get; set; }
    public decimal AbsoluteDelta { get; set; }
    public decimal? RelativeDelta { get; set; }
    public DateOnly CurrentPeriodStart { get; set; }
    public DateOnly CurrentPeriodEnd { get; set; }
    public DateOnly ComparisonPeriodStart { get; set; }
    public DateOnly ComparisonPeriodEnd { get; set; }
    public required string EvidenceSignalIdsJson { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public required string Confidence { get; set; }
}
