using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class FeedbackKinds
{
    public const string OpportunityRating = "opportunity-rating";
    public const string IncorrectContext = "incorrect-context";
    public const string UnsafeGuidance = "unsafe-guidance";
    public const string GeneralFeedback = "general-feedback";
    public const string SupportRequest = "support-request";

    public static bool IsAllowed(string? value) =>
        value is OpportunityRating or IncorrectContext or UnsafeGuidance or GeneralFeedback or SupportRequest;
}

public static class FeedbackUsefulnessValues
{
    public const string Useful = "useful";
    public const string NotUseful = "not-useful";

    public static bool IsAllowed(string? value) => value is Useful or NotUseful;
}

public sealed record SubmitFeedbackRequest(
    string Kind,
    Guid? OpportunityId,
    string? ContextKey,
    string? Usefulness,
    string? Message);

public sealed record FeedbackReceipt(Guid Id, string Kind, DateTimeOffset CreatedAt);

public sealed class FeedbackRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid SubmittedByAccountId { get; set; }
    public required string Kind { get; set; }
    public Guid? OpportunityId { get; set; }
    public string? ContextKey { get; set; }
    public string? Usefulness { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public static class FeedbackPolicy
{
    public static Dictionary<string, string[]> Validate(SubmitFeedbackRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var kind = request.Kind?.Trim();
        var contextKey = NormalizeContextKey(request.ContextKey);
        var usefulness = request.Usefulness?.Trim();
        var message = NormalizeMessage(request.Message);

        if (!FeedbackKinds.IsAllowed(kind))
            errors[nameof(request.Kind)] = ["Feedback kind is not supported."];

        if (contextKey is { Length: > 120 })
            errors[nameof(request.ContextKey)] = ["Context key must be 120 characters or fewer."];

        if (message is { Length: > 1200 })
            errors[nameof(request.Message)] = ["Feedback message must be 1200 characters or fewer."];

        switch (kind)
        {
            case FeedbackKinds.OpportunityRating:
                if (request.OpportunityId is null)
                    errors[nameof(request.OpportunityId)] = ["Opportunity is required for a usefulness rating."];
                if (!FeedbackUsefulnessValues.IsAllowed(usefulness))
                    errors[nameof(request.Usefulness)] = ["Usefulness must be useful or not-useful."];
                if (contextKey is not null)
                    errors[nameof(request.ContextKey)] = ["Context key is not supported for an Opportunity rating."];
                break;

            case FeedbackKinds.UnsafeGuidance:
                if (request.OpportunityId is null)
                    errors[nameof(request.OpportunityId)] = ["Opportunity is required for an unsafe-guidance report."];
                if (usefulness is not null)
                    errors[nameof(request.Usefulness)] = ["Usefulness is not supported for an unsafe-guidance report."];
                if (contextKey is not null)
                    errors[nameof(request.ContextKey)] = ["Context key is not supported for an unsafe-guidance report."];
                break;

            case FeedbackKinds.IncorrectContext:
                if (usefulness is not null)
                    errors[nameof(request.Usefulness)] = ["Usefulness is not supported for an incorrect-context report."];
                break;

            case FeedbackKinds.GeneralFeedback:
            case FeedbackKinds.SupportRequest:
                if (usefulness is not null)
                    errors[nameof(request.Usefulness)] = ["Usefulness is not supported for this feedback kind."];
                if (contextKey is not null)
                    errors[nameof(request.ContextKey)] = ["Context key is not supported for this feedback kind."];
                break;
        }

        return errors;
    }

    public static string? NormalizeMessage(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static string? NormalizeContextKey(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

public sealed class FeedbackValidationException(Dictionary<string, string[]> errors) : Exception("Feedback is invalid.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}

public static class FeedbackService
{
    public static async Task<FeedbackReceipt?> SubmitAsync(
        AtlasDbContext db,
        Guid businessId,
        UserAccount account,
        SubmitFeedbackRequest request,
        CancellationToken ct)
    {
        var errors = FeedbackPolicy.Validate(request);
        if (errors.Count > 0) throw new FeedbackValidationException(errors);

        if (request.OpportunityId is Guid opportunityId)
        {
            var exists = await db.Opportunities.AnyAsync(
                x => x.Id == opportunityId && x.BusinessId == businessId,
                ct);
            if (!exists) return null;
        }

        var record = new FeedbackRecord
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SubmittedByAccountId = account.Id,
            Kind = request.Kind.Trim(),
            OpportunityId = request.OpportunityId,
            ContextKey = FeedbackPolicy.NormalizeContextKey(request.ContextKey),
            Usefulness = string.IsNullOrWhiteSpace(request.Usefulness) ? null : request.Usefulness.Trim(),
            Message = FeedbackPolicy.NormalizeMessage(request.Message),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.FeedbackRecords.Add(record);
        db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"feedback.submitted:{record.Kind}"));
        await db.SaveChangesAsync(ct);
        return new FeedbackReceipt(record.Id, record.Kind, record.CreatedAt);
    }
}

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/businesses/{businessId:guid}/feedback", async (
            Guid businessId,
            SubmitFeedbackRequest request,
            ClaimsPrincipal user,
            AtlasDbContext db,
            CancellationToken ct) =>
        {
            var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(subject)) return Results.NotFound();

            var membership = await db.BusinessMemberships
                .Include(x => x.UserAccount)
                .SingleOrDefaultAsync(
                    x => x.BusinessId == businessId &&
                         x.UserAccount.ProviderSubject == subject &&
                         x.Role == MembershipRoles.BusinessOwner,
                    ct);
            if (membership is null) return Results.NotFound();

            var errors = FeedbackPolicy.Validate(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(
                    errors,
                    extensions: new Dictionary<string, object?> { ["code"] = "feedback_invalid" });

            var receipt = await FeedbackService.SubmitAsync(db, businessId, membership.UserAccount, request, ct);
            if (receipt is null) return Results.NotFound();

            return Results.Created($"/api/v1/businesses/{businessId}/feedback", receipt);
        }).RequireAuthorization("BusinessOwner");
    }
}