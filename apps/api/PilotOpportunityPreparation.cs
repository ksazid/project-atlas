using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed record PilotOpportunityCandidate(
    Guid GoalId,
    string PatternKey,
    string Title,
    string Confidence,
    string Effort,
    string BundleFingerprint,
    string KnowledgePackKey,
    string KnowledgePackVersion,
    int EvidenceCount);

public sealed record PilotPrepareOpportunityRequest(
    string PatternKey,
    string BundleFingerprint,
    string Reason);

public static class PilotPreparationStates
{
    public const string Prepared = "prepared";
    public const string Stale = "stale";
    public const string Conflict = "conflict";
    public const string NotReady = "not-ready";
    public const string Invalid = "invalid";
}

public sealed record PilotPrepareOpportunityResult(string State, string? Code, Guid? OpportunityId);

public static class PilotOpportunityPreparationPolicy
{
    public static Dictionary<string, string[]> Validate(PilotPrepareOpportunityRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.PatternKey) || request.PatternKey.Trim().Length > 160)
            errors[nameof(request.PatternKey)] = ["Pattern key is required and must be 160 characters or fewer."];
        if (string.IsNullOrWhiteSpace(request.BundleFingerprint) || request.BundleFingerprint.Trim().Length > 256)
            errors[nameof(request.BundleFingerprint)] = ["Bundle fingerprint is required and must be 256 characters or fewer."];
        var reason = PilotOperationsPolicy.NormalizeText(request.Reason);
        if (reason is null)
            errors[nameof(request.Reason)] = ["Preparation reason is required."];
        else if (reason.Length > 2000)
            errors[nameof(request.Reason)] = ["Preparation reason must be 2000 characters or fewer."];
        return errors;
    }
}

public static partial class PilotOperationsService
{
    private sealed record PilotPreparationContext(
        BusinessProfile Profile,
        IReadOnlyList<BusinessGoal> Goals,
        BusinessKnowledgeAssignment Assignment,
        ResolvedKnowledgeBundle Bundle,
        IReadOnlyList<Opportunity> PriorOpportunities);

    public static async Task<PilotOpportunityCandidate?> PreviewOpportunityAsync(
        AtlasDbContext db,
        Guid businessId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var context = await BuildPreparationContextAsync(db, businessId, ct);
        if (context is null) return null;
        var candidate = OpportunityFocusService.SelectEligibleCandidate(
            OpportunityGenerator.Generate(context.Profile, context.Goals, context.Bundle, context.PriorOpportunities, now));
        return candidate is null ? null : ToPilotCandidate(candidate);
    }

    public static async Task<PilotPrepareOpportunityResult> PrepareOpportunityAsync(
        AtlasDbContext db,
        Guid businessId,
        UserAccount operatorAccount,
        PilotPrepareOpportunityRequest request,
        DateTimeOffset now,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(operatorAccount);
        if (PilotOpportunityPreparationPolicy.Validate(request).Count > 0)
            return new(PilotPreparationStates.Invalid, "pilot_opportunity_prepare_invalid", null);
        if (!await db.Businesses.AnyAsync(x => x.Id == businessId, ct))
            return new(PilotPreparationStates.NotReady, "pilot_business_not_found", null);

        var active = await db.Opportunities.AnyAsync(
            x => x.BusinessId == businessId && x.Status == OpportunityStatuses.Available && x.ExpiresAt > now,
            ct);
        if (active)
            return new(PilotPreparationStates.Conflict, "pilot_active_opportunity_exists", null);

        var expiredAvailable = await db.Opportunities
            .Where(x => x.BusinessId == businessId && x.Status == OpportunityStatuses.Available && x.ExpiresAt <= now)
            .ToListAsync(ct);
        foreach (var value in expiredAvailable) value.Status = OpportunityStatuses.Expired;

        var context = await BuildPreparationContextAsync(db, businessId, ct);
        if (context is null)
            return new(PilotPreparationStates.NotReady, "pilot_opportunity_not_ready", null);

        var candidate = OpportunityFocusService.SelectEligibleCandidate(
            OpportunityGenerator.Generate(context.Profile, context.Goals, context.Bundle, context.PriorOpportunities, now));
        if (candidate is null)
            return new(PilotPreparationStates.NotReady, "pilot_opportunity_no_candidate", null);

        if (!string.Equals(candidate.PatternKey, request.PatternKey.Trim(), StringComparison.Ordinal) ||
            !string.Equals(candidate.BundleFingerprint, request.BundleFingerprint.Trim(), StringComparison.Ordinal))
            return new(PilotPreparationStates.Stale, "pilot_opportunity_candidate_stale", null);

        var opportunity = new Opportunity
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            GoalId = candidate.GoalId,
            Title = candidate.Title,
            WhyItMatters = candidate.Reason,
            WhyNow = candidate.WhyNow,
            ExpectedImpact = candidate.ExpectedImpact,
            Effort = candidate.Effort,
            Confidence = candidate.Confidence,
            EvidenceSummary = EvidenceSummary(candidate),
            EvidenceJson = OpportunityGenerationSnapshot.Serialize(candidate),
            Status = OpportunityStatuses.Available,
            KnowledgePackKey = candidate.KnowledgePackKey,
            KnowledgePackVersion = candidate.KnowledgePackVersion,
            KnowledgePackVersionId = context.Assignment.KnowledgePackVersionId,
            CreatedAt = now,
            ExpiresAt = candidate.ExpiresAt
        };

        db.Opportunities.Add(opportunity);
        db.PilotOperationRecords.Add(new PilotOperationRecord
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            OperatorUserAccountId = operatorAccount.Id,
            Action = PilotOperationActions.OpportunityPrepared,
            TargetType = nameof(Opportunity),
            TargetId = opportunity.Id,
            Reason = PilotOperationsPolicy.NormalizeText(request.Reason),
            MetadataJson = JsonSerializer.Serialize(new
            {
                patternKey = candidate.PatternKey,
                bundleFingerprint = candidate.BundleFingerprint,
                knowledgePackKey = candidate.KnowledgePackKey,
                knowledgePackVersion = candidate.KnowledgePackVersion
            }),
            OccurredAt = now
        });
        db.AuditRecords.Add(AuditRecord.Create(operatorAccount.Id, businessId, $"pilot-operations.opportunity.prepared:{opportunity.Id}"));
        await db.SaveChangesAsync(ct);
        return new(PilotPreparationStates.Prepared, null, opportunity.Id);
    }

    private static async Task<PilotPreparationContext?> BuildPreparationContextAsync(
        AtlasDbContext db,
        Guid businessId,
        CancellationToken ct)
    {
        var business = await db.Businesses.SingleOrDefaultAsync(x => x.Id == businessId, ct);
        var profile = await db.BusinessProfiles.SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);
        var goals = await db.BusinessGoals
            .Where(x => x.BusinessId == businessId)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
        var assignment = await db.BusinessKnowledgeAssignments.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.IsCurrent, ct);
        if (business is null || profile is not { OwnerConfirmed: true } || goals.Count == 0 || assignment is not { IsCurrent: true })
            return null;

        var profileFields = await db.BusinessProfileFields.Where(x => x.BusinessId == businessId).ToListAsync(ct);
        var contextEntries = await db.BusinessContextEntries.Where(x => x.BusinessId == businessId).ToListAsync(ct);
        var memoryItems = await db.BusinessMemoryItems.Where(x => x.BusinessId == businessId).ToListAsync(ct);
        var prior = await db.Opportunities.Where(x => x.BusinessId == businessId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

        try
        {
            var bundle = KnowledgeBundleResolver.Resolve(business, assignment, profileFields, contextEntries, memoryItems);
            return new(profile, goals, assignment, bundle, prior);
        }
        catch (KnowledgeBundleResolutionException)
        {
            return null;
        }
    }

    private static PilotOpportunityCandidate ToPilotCandidate(GeneratedOpportunityCandidate candidate) => new(
        candidate.GoalId,
        candidate.PatternKey,
        candidate.Title,
        candidate.Confidence,
        candidate.Effort,
        candidate.BundleFingerprint,
        candidate.KnowledgePackKey,
        candidate.KnowledgePackVersion,
        candidate.Evidence.Count(x => !string.Equals(x.Layer, "policy", StringComparison.OrdinalIgnoreCase)));

    private static string EvidenceSummary(GeneratedOpportunityCandidate candidate)
    {
        var factualEvidenceCount = candidate.Evidence.Count(x => !string.Equals(x.Layer, "policy", StringComparison.OrdinalIgnoreCase));
        var evidenceLabel = factualEvidenceCount == 1 ? "1 evidence item" : $"{factualEvidenceCount} evidence items";
        return $"{evidenceLabel}; priority goal #{candidate.GoalPriority}: {candidate.GoalTitle}; {candidate.KnowledgePackKey} v{candidate.KnowledgePackVersion}.";
    }
}
