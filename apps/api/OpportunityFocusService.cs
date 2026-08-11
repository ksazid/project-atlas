using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class OpportunityFocusGenerationStates
{
    public const string Ready = "ready";
    public const string NoFocus = "no-focus";
    public const string InsufficientContext = "insufficient-context";
    public const string Degraded = "degraded";
}

public sealed record OpportunityFocusGenerationResult(
    string State,
    Opportunity? Opportunity,
    string? Code,
    string Message);

public static class OpportunityFocusService
{
    public static async Task<OpportunityFocusGenerationResult> GenerateAsync(
        AtlasDbContext db,
        Guid businessId,
        Guid actorUserAccountId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);

        var ownerMembershipExists = await db.BusinessMemberships.AnyAsync(x =>
            x.BusinessId == businessId &&
            x.UserAccountId == actorUserAccountId &&
            x.Role == MembershipRoles.BusinessOwner, ct);
        if (!ownerMembershipExists)
            return Degraded("business_access_unavailable", "Atlas could not safely resolve this Business for the current owner.");

        var current = await db.Set<Opportunity>()
            .Where(x => x.BusinessId == businessId && x.Status == OpportunityStatuses.Available)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (current is not null && current.ExpiresAt > now)
            return Ready(current);

        if (current is not null)
            current.Status = OpportunityStatuses.Expired;

        var business = await db.Businesses.SingleOrDefaultAsync(x => x.Id == businessId, ct);
        var profile = await db.BusinessProfiles.SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);
        var goals = await db.BusinessGoals
            .Where(x => x.BusinessId == businessId)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
        var assignment = await db.BusinessKnowledgeAssignments
            .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.IsCurrent, ct);

        if (business is null || !OpportunityPolicy.IsEligible(profile, goals, assignment))
        {
            await SaveStatusChangeIfNeeded(db, current, ct);
            return new OpportunityFocusGenerationResult(
                OpportunityFocusGenerationStates.InsufficientContext,
                null,
                "opportunity_context_incomplete",
                "Confirm your Business Profile, choose at least one goal and keep an active Knowledge Pack to receive Today’s Focus.");
        }

        var profileFields = await db.BusinessProfileFields
            .Where(x => x.BusinessId == businessId)
            .ToListAsync(ct);
        var contextEntries = await db.BusinessContextEntries
            .Where(x => x.BusinessId == businessId)
            .ToListAsync(ct);
        var memoryItems = await db.BusinessMemoryItems
            .Where(x => x.BusinessId == businessId)
            .ToListAsync(ct);
        var priorOpportunities = await db.Set<Opportunity>()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        ResolvedKnowledgeBundle bundle;
        try
        {
            bundle = KnowledgeBundleResolver.Resolve(
                business,
                assignment!,
                profileFields,
                contextEntries,
                memoryItems);
        }
        catch (KnowledgeBundleResolutionException ex)
        {
            await SaveStatusChangeIfNeeded(db, current, ct);
            return Degraded(ex.Code, "Atlas could not safely resolve the current intelligence inputs. Review the Business setup and try again.");
        }

        var generation = OpportunityGenerator.Generate(profile, goals, bundle, priorOpportunities, now);
        if (generation.Selected is null)
        {
            await SaveStatusChangeIfNeeded(db, current, ct);
            return new OpportunityFocusGenerationResult(
                OpportunityFocusGenerationStates.NoFocus,
                null,
                "opportunity_no_eligible_candidate",
                "Atlas does not have enough evidence for a useful recommendation yet. Add or confirm relevant Business Context instead of receiving filler guidance.");
        }

        var candidate = generation.Selected;
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
            KnowledgePackVersionId = assignment!.KnowledgePackVersionId,
            CreatedAt = now,
            ExpiresAt = candidate.ExpiresAt
        };

        db.Set<Opportunity>().Add(opportunity);
        db.AuditRecords.Add(AuditRecord.Create(actorUserAccountId, businessId, $"opportunity.created:{opportunity.Id}:{candidate.PatternKey}"));
        await db.SaveChangesAsync(ct);
        return Ready(opportunity);
    }

    private static string EvidenceSummary(GeneratedOpportunityCandidate candidate)
    {
        var factualEvidenceCount = candidate.Evidence.Count(x => x.Layer != "policy");
        var evidenceLabel = factualEvidenceCount == 1 ? "1 evidence item" : $"{factualEvidenceCount} evidence items";
        return $"{evidenceLabel}; priority goal #{candidate.GoalPriority}: {candidate.GoalTitle}; {candidate.KnowledgePackKey} v{candidate.KnowledgePackVersion}.";
    }

    private static async Task SaveStatusChangeIfNeeded(AtlasDbContext db, Opportunity? expired, CancellationToken ct)
    {
        if (expired is not null) await db.SaveChangesAsync(ct);
    }

    private static OpportunityFocusGenerationResult Ready(Opportunity opportunity) => new(
        OpportunityFocusGenerationStates.Ready,
        opportunity,
        null,
        "Today’s Focus is ready.");

    private static OpportunityFocusGenerationResult Degraded(string code, string message) => new(
        OpportunityFocusGenerationStates.Degraded,
        null,
        code,
        message);
}
