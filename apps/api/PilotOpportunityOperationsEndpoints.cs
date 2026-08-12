using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class PilotOpportunityStatuses
{
    public const string Withdrawn = "withdrawn";
}

public static class PilotWithdrawalStates
{
    public const string Withdrawn = "withdrawn";
    public const string Stale = "stale";
    public const string Conflict = "conflict";
    public const string NotFound = "not-found";
    public const string Invalid = "invalid";
}

public sealed record PilotWithdrawOpportunityResult(string State, string? Code);

public static partial class PilotOperationsService
{
    public static async Task<PilotWithdrawOpportunityResult> WithdrawOpportunityAsync(
        AtlasDbContext db,
        Guid businessId,
        Guid opportunityId,
        UserAccount operatorAccount,
        PilotWithdrawRequest request,
        DateTimeOffset now,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(operatorAccount);

        if (PilotOperationsPolicy.ValidateWithdrawal(request).Count > 0)
            return new(PilotWithdrawalStates.Invalid, "pilot_opportunity_withdraw_invalid");

        var opportunity = await db.Opportunities.SingleOrDefaultAsync(
            x => x.Id == opportunityId && x.BusinessId == businessId,
            ct);
        if (opportunity is null)
            return new(PilotWithdrawalStates.NotFound, "pilot_opportunity_not_found");

        if (opportunity.ConcurrencyVersion != request.Version)
            return new(PilotWithdrawalStates.Stale, "pilot_opportunity_stale");

        if (opportunity.Status != OpportunityStatuses.Available || opportunity.ExpiresAt <= now)
            return new(PilotWithdrawalStates.Conflict, "pilot_opportunity_not_withdrawable");

        var reason = PilotOperationsPolicy.NormalizeText(request.Reason)!;
        opportunity.Status = PilotOpportunityStatuses.Withdrawn;
        opportunity.DecidedAt = now;
        opportunity.DecidedByUserAccountId = operatorAccount.Id;
        opportunity.DecisionReason = reason;

        db.PilotOperationRecords.Add(new PilotOperationRecord
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            OperatorUserAccountId = operatorAccount.Id,
            Action = PilotOperationActions.OpportunityWithdrawn,
            TargetType = nameof(Opportunity),
            TargetId = opportunity.Id,
            Reason = reason,
            MetadataJson = JsonSerializer.Serialize(new
            {
                previousStatus = OpportunityStatuses.Available,
                requestedVersion = request.Version
            }),
            OccurredAt = now
        });
        db.AuditRecords.Add(AuditRecord.Create(
            operatorAccount.Id,
            businessId,
            $"pilot-operations.opportunity.withdrawn:{opportunity.Id}"));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(PilotWithdrawalStates.Stale, "pilot_opportunity_stale");
        }

        return new(PilotWithdrawalStates.Withdrawn, null);
    }
}

public static class PilotOpportunityOperationsEndpoints
{
    public static void MapPilotOpportunityOperationsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/pilot-operations/businesses/{businessId:guid}/opportunity-candidate", async (
            Guid businessId,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var account = await PilotOperationsService.ResolveOperatorAsync(db, user, DateTimeOffset.UtcNow, ct);
            if (account is null) return Results.Unauthorized();

            var candidate = await PilotOperationsService.PreviewOpportunityAsync(db, businessId, DateTimeOffset.UtcNow, ct);
            return candidate is null
                ? Results.Ok(new { state = PilotPreparationStates.NotReady, code = "pilot_opportunity_not_ready" })
                : Results.Ok(new { state = "ready", candidate });
        }).RequireAuthorization("InternalOperator");

        app.MapPost("/api/v1/pilot-operations/businesses/{businessId:guid}/opportunities", async (
            Guid businessId,
            PilotPrepareOpportunityRequest request,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var errors = PilotOpportunityPreparationPolicy.Validate(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors, extensions: new Dictionary<string, object?> { ["code"] = "pilot_opportunity_prepare_invalid" });

            var account = await PilotOperationsService.ResolveOperatorAsync(db, user, DateTimeOffset.UtcNow, ct);
            if (account is null) return Results.Unauthorized();

            var result = await PilotOperationsService.PrepareOpportunityAsync(
                db, businessId, account, request, DateTimeOffset.UtcNow, ct);
            return result.State switch
            {
                PilotPreparationStates.Prepared when result.OpportunityId is Guid id =>
                    Results.Created($"/api/v1/pilot-operations/businesses/{businessId}/opportunities/{id}", result),
                PilotPreparationStates.Stale => Results.Conflict(result),
                PilotPreparationStates.Conflict => Results.Conflict(result),
                PilotPreparationStates.Invalid => Results.BadRequest(result),
                _ => Results.UnprocessableEntity(result)
            };
        }).RequireAuthorization("InternalOperator");

        app.MapPost("/api/v1/pilot-operations/businesses/{businessId:guid}/opportunities/{opportunityId:guid}/withdraw", async (
            Guid businessId,
            Guid opportunityId,
            PilotWithdrawRequest request,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var errors = PilotOperationsPolicy.ValidateWithdrawal(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors, extensions: new Dictionary<string, object?> { ["code"] = "pilot_opportunity_withdraw_invalid" });

            var account = await PilotOperationsService.ResolveOperatorAsync(db, user, DateTimeOffset.UtcNow, ct);
            if (account is null) return Results.Unauthorized();

            var result = await PilotOperationsService.WithdrawOpportunityAsync(
                db, businessId, opportunityId, account, request, DateTimeOffset.UtcNow, ct);
            return result.State switch
            {
                PilotWithdrawalStates.Withdrawn => Results.Ok(result),
                PilotWithdrawalStates.NotFound => Results.NotFound(result),
                PilotWithdrawalStates.Stale => Results.Conflict(result),
                PilotWithdrawalStates.Conflict => Results.Conflict(result),
                _ => Results.BadRequest(result)
            };
        }).RequireAuthorization("InternalOperator");
    }
}
