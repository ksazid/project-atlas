using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class NotificationPolicyTests
{
    [Fact]
    public void TodayFocusStableKey_IsDeterministicPerOpportunity()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Assert.Equal(NotificationPolicy.TodayFocusStableKey(id), NotificationPolicy.TodayFocusStableKey(id));
    }

    [Fact]
    public void OutcomeFollowUpStableKey_ChangesWhenFollowUpChanges()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var first = new DateTimeOffset(2026, 8, 8, 8, 0, 0, TimeSpan.Zero);
        var second = first.AddDays(1);

        Assert.NotEqual(
            NotificationPolicy.OutcomeFollowUpStableKey(id, first),
            NotificationPolicy.OutcomeFollowUpStableKey(id, second));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("/weekly-review", true)]
    [InlineData("/history", true)]
    [InlineData("/opportunities/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", true)]
    [InlineData("https://example.com", false)]
    [InlineData("//example.com", false)]
    [InlineData("/settings", false)]
    public void DeepLinks_AreRestrictedToApprovedAtlasRoutes(string? deepLink, bool expected)
    {
        Assert.Equal(expected, NotificationPolicy.IsSafeDeepLink(deepLink));
    }

    [Fact]
    public void DefaultPreferences_EnableAllInAppNotificationCategories()
    {
        var preferences = NotificationPolicy.DefaultPreferences();

        Assert.True(preferences.TodayFocusEnabled);
        Assert.True(preferences.OutcomeFollowUpEnabled);
        Assert.True(preferences.WeeklyReviewEnabled);
    }
}
