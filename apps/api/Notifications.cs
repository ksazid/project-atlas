using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public static class NotificationCategories
{
    public const string TodayFocus = "today-focus";
    public const string OutcomeFollowUp = "outcome-follow-up";
    public const string WeeklyReview = "weekly-review";

    public static bool IsValid(string value) => value is TodayFocus or OutcomeFollowUp or WeeklyReview;
}

public sealed class NotificationPreference
{
    public Guid BusinessId { get; set; }
    public bool TodayFocusEnabled { get; set; } = true;
    public bool OutcomeFollowUpEnabled { get; set; } = true;
    public bool WeeklyReviewEnabled { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
    public uint ConcurrencyVersion { get; set; }
}

public sealed class NotificationRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string StableKey { get; set; }
    public required string Category { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public string? DeepLink { get; set; }
    public Guid? SourceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public uint ConcurrencyVersion { get; set; }
}

public sealed record NotificationPreferenceResponse(
    bool TodayFocusEnabled,
    bool OutcomeFollowUpEnabled,
    bool WeeklyReviewEnabled,
    uint Version);

public sealed record UpdateNotificationPreferenceRequest(
    bool TodayFocusEnabled,
    bool OutcomeFollowUpEnabled,
    bool WeeklyReviewEnabled,
    uint? Version);

public sealed record NotificationItemResponse(
    Guid Id,
    string Category,
    string Title,
    string Body,
    string? DeepLink,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    uint Version);

public sealed record NotificationCenterResponse(
    IReadOnlyList<NotificationItemResponse> Items,
    int UnreadCount,
    NotificationPreferenceResponse Preferences);

public sealed record MarkNotificationReadRequest(uint Version);

public static class NotificationPolicy
{
    public static NotificationPreferenceResponse DefaultPreferences() => new(true, true, true, 0);

    public static string WeeklyReviewStableKey(DateTimeOffset now)
    {
        var utcDate = now.UtcDateTime.Date;
        var year = ISOWeek.GetYear(utcDate);
        var week = ISOWeek.GetWeekOfYear(utcDate);
        return $"weekly-review:{year}-W{week:D2}";
    }

    public static string TodayFocusStableKey(Guid opportunityId) => $"today-focus:{opportunityId:N}";
    public static string OutcomeFollowUpStableKey(Guid outcomeId, DateTimeOffset followUpAt) => $"outcome-follow-up:{outcomeId:N}:{followUpAt.UtcTicks}";

    public static bool IsSafeDeepLink(string? value) =>
        value is null || value == "/weekly-review" || value == "/history" || value.StartsWith("/opportunities/", StringComparison.Ordinal);
}

public static class NotificationEndpoints
{
    private static string? Subject(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

    private static async Task<UserAccount?> OwnerAccount(Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        var subject = Subject(user);
        if (string.IsNullOrWhiteSpace(subject)) return null;
        var membership = await db.BusinessMemberships.Include(x => x.UserAccount)
            .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.UserAccount.ProviderSubject == subject && x.Role == MembershipRoles.BusinessOwner, ct);
        return membership?.UserAccount;
    }

    private static NotificationPreferenceResponse PreferenceResponse(NotificationPreference? value) =>
        value is null
            ? NotificationPolicy.DefaultPreferences()
            : new NotificationPreferenceResponse(value.TodayFocusEnabled, value.OutcomeFollowUpEnabled, value.WeeklyReviewEnabled, value.ConcurrencyVersion);

    private static async Task Materialize(Guid businessId, NotificationPreferenceResponse preferences, AtlasDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = new List<NotificationRecord>();

        if (preferences.TodayFocusEnabled)
        {
            var focus = await db.Set<Opportunity>().AsNoTracking()
                .Where(x => x.BusinessId == businessId && x.Status == OpportunityStatuses.Available && x.ExpiresAt > now)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (focus is not null)
            {
                candidates.Add(new NotificationRecord
                {
                    Id = Guid.NewGuid(), BusinessId = businessId,
                    StableKey = NotificationPolicy.TodayFocusStableKey(focus.Id),
                    Category = NotificationCategories.TodayFocus,
                    Title = "Today’s Focus is ready",
                    Body = focus.Title,
                    DeepLink = $"/opportunities/{focus.Id}", SourceId = focus.Id, CreatedAt = now
                });
            }
        }

        if (preferences.OutcomeFollowUpEnabled)
        {
            var dueOutcomes = await db.Outcomes.AsNoTracking()
                .Where(x => x.BusinessId == businessId && x.FollowUpAt != null && x.FollowUpAt <= now)
                .OrderBy(x => x.FollowUpAt)
                .Take(25)
                .ToListAsync(ct);
            var opportunityTitles = await db.Set<Opportunity>().AsNoTracking()
                .Where(x => x.BusinessId == businessId && dueOutcomes.Select(o => o.OpportunityId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Title, ct);
            foreach (var outcome in dueOutcomes)
            {
                var due = outcome.FollowUpAt!.Value;
                opportunityTitles.TryGetValue(outcome.OpportunityId, out var title);
                candidates.Add(new NotificationRecord
                {
                    Id = Guid.NewGuid(), BusinessId = businessId,
                    StableKey = NotificationPolicy.OutcomeFollowUpStableKey(outcome.Id, due),
                    Category = NotificationCategories.OutcomeFollowUp,
                    Title = "Outcome follow-up is due",
                    Body = title is null ? "Review the recorded Outcome and add any new evidence." : $"Review the Outcome for {title}.",
                    DeepLink = $"/opportunities/{outcome.OpportunityId}", SourceId = outcome.Id, CreatedAt = now
                });
            }
        }

        if (preferences.WeeklyReviewEnabled)
        {
            candidates.Add(new NotificationRecord
            {
                Id = Guid.NewGuid(), BusinessId = businessId,
                StableKey = NotificationPolicy.WeeklyReviewStableKey(now),
                Category = NotificationCategories.WeeklyReview,
                Title = "Your Weekly Review is available",
                Body = "Review the last seven days of recorded Opportunities, Actions, Execution Kit usage and Outcomes.",
                DeepLink = "/weekly-review", CreatedAt = now
            });
        }

        if (candidates.Count == 0) return;
        var keys = candidates.Select(x => x.StableKey).ToArray();
        var existing = await db.NotificationRecords.AsNoTracking()
            .Where(x => x.BusinessId == businessId && keys.Contains(x.StableKey))
            .Select(x => x.StableKey)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);
        var missing = candidates.Where(x => !existingSet.Contains(x.StableKey) && NotificationPolicy.IsSafeDeepLink(x.DeepLink)).ToList();
        if (missing.Count == 0) return;
        db.NotificationRecords.AddRange(missing);
        await db.SaveChangesAsync(ct);
    }

    private static async Task<NotificationCenterResponse> Center(Guid businessId, AtlasDbContext db, CancellationToken ct)
    {
        var preference = await db.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);
        var preferences = PreferenceResponse(preference);
        await Materialize(businessId, preferences, db, ct);
        var items = await db.NotificationRecords.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Take(100)
            .Select(x => new NotificationItemResponse(x.Id, x.Category, x.Title, x.Body, x.DeepLink, x.CreatedAt, x.ReadAt, x.ConcurrencyVersion))
            .ToListAsync(ct);
        return new NotificationCenterResponse(items, items.Count(x => x.ReadAt is null), preferences);
    }

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/businesses/{businessId:guid}/notifications", async (
            Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            if (await OwnerAccount(businessId, user, db, ct) is null) return Results.NotFound();
            return Results.Ok(await Center(businessId, db, ct));
        }).RequireAuthorization("BusinessOwner");

        app.MapGet("/api/v1/businesses/{businessId:guid}/notification-preferences", async (
            Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            if (await OwnerAccount(businessId, user, db, ct) is null) return Results.NotFound();
            return Results.Ok(PreferenceResponse(await db.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.BusinessId == businessId, ct)));
        }).RequireAuthorization("BusinessOwner");

        app.MapPut("/api/v1/businesses/{businessId:guid}/notification-preferences", async (
            Guid businessId, UpdateNotificationPreferenceRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();
            var preference = await db.NotificationPreferences.SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);
            if (preference is null)
            {
                if (request.Version is > 0)
                    return Results.Conflict(new { code = "notification_preferences_stale", message = "Notification preferences changed. Refresh before saving." });
                preference = new NotificationPreference { BusinessId = businessId, UpdatedAt = DateTimeOffset.UtcNow };
                db.NotificationPreferences.Add(preference);
            }
            else if (request.Version is null || request.Version.Value != preference.ConcurrencyVersion)
            {
                return Results.Conflict(new { code = "notification_preferences_stale", message = "Notification preferences changed. Refresh before saving." });
            }

            preference.TodayFocusEnabled = request.TodayFocusEnabled;
            preference.OutcomeFollowUpEnabled = request.OutcomeFollowUpEnabled;
            preference.WeeklyReviewEnabled = request.WeeklyReviewEnabled;
            preference.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, "notifications.preferences.updated"));
            await db.SaveChangesAsync(ct);
            return Results.Ok(PreferenceResponse(preference));
        }).RequireAuthorization("BusinessOwner");

        app.MapPut("/api/v1/businesses/{businessId:guid}/notifications/{notificationId:guid}/read", async (
            Guid businessId, Guid notificationId, MarkNotificationReadRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();
            var notification = await db.NotificationRecords.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == notificationId, ct);
            if (notification is null) return Results.NotFound();
            if (notification.ConcurrencyVersion != request.Version)
                return Results.Conflict(new { code = "notification_stale", message = "The notification changed. Refresh before updating it." });
            if (notification.ReadAt is null)
            {
                notification.ReadAt = DateTimeOffset.UtcNow;
                db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"notification.read:{notification.Id}"));
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok(new NotificationItemResponse(notification.Id, notification.Category, notification.Title, notification.Body, notification.DeepLink, notification.CreatedAt, notification.ReadAt, notification.ConcurrencyVersion));
        }).RequireAuthorization("BusinessOwner");

        app.MapPost("/api/v1/businesses/{businessId:guid}/notifications/read-all", async (
            Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
        {
            var account = await OwnerAccount(businessId, user, db, ct);
            if (account is null) return Results.NotFound();
            var unread = await db.NotificationRecords.Where(x => x.BusinessId == businessId && x.ReadAt == null).ToListAsync(ct);
            if (unread.Count > 0)
            {
                var now = DateTimeOffset.UtcNow;
                foreach (var notification in unread) notification.ReadAt = now;
                db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, $"notifications.read-all:{unread.Count}"));
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok(await Center(businessId, db, ct));
        }).RequireAuthorization("BusinessOwner");

        return app;
    }
}
