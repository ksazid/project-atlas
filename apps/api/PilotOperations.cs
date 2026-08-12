namespace Atlas.Api;

public sealed class IntelligenceRunRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? ActorUserAccountId { get; set; }
    public required string Outcome { get; set; }
    public string? Code { get; set; }
    public int CandidateCount { get; set; }
    public Guid? OpportunityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class PilotOperationRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OperatorUserAccountId { get; set; }
    public required string Action { get; set; }
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? Reason { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public static class PilotOperationActions
{
    public const string SupportNote = "support-note";
    public const string ProfileCorrection = "profile-correction";
    public const string OpportunityPrepared = "opportunity-prepared";
    public const string OpportunityWithdrawn = "opportunity-withdrawn";
}

public sealed record PilotSupportNoteRequest(string Note);
public sealed record PilotWithdrawRequest(string Reason, uint Version);

public static class PilotOperationsPolicy
{
    public static Dictionary<string, string[]> ValidateSupportNote(PilotSupportNoteRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var note = NormalizeText(request.Note);
        if (note is null)
            errors[nameof(request.Note)] = ["Support note is required."];
        else if (note.Length > 2000)
            errors[nameof(request.Note)] = ["Support note must be 2000 characters or fewer."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateWithdrawal(PilotWithdrawRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var reason = NormalizeText(request.Reason);
        if (reason is null)
            errors[nameof(request.Reason)] = ["Withdrawal reason is required."];
        else if (reason.Length > 2000)
            errors[nameof(request.Reason)] = ["Withdrawal reason must be 2000 characters or fewer."];
        return errors;
    }

    public static string? NormalizeText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
