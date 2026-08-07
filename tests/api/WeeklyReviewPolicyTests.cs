using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class WeeklyReviewPolicyTests
{
    [Fact]
    public void Window_is_exactly_seven_days()
    {
        var end = new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);
        var (start, actualEnd) = WeeklyReviewPolicy.Window(end);

        Assert.Equal(end, actualEnd);
        Assert.Equal(TimeSpan.FromDays(7), actualEnd - start);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 1)]
    [InlineData(2, 1, 1, 0, 3)]
    [InlineData(1, 0, 1, 4, 3)]
    public void Highlights_report_only_recorded_activity(int completed, int outcomes, int missing, int assets, int minimumExpected)
    {
        var counts = new WeeklyReviewCounts(3, 0, completed, 0, 0, 0, outcomes, missing, assets);
        var highlights = WeeklyReviewPolicy.Highlights(counts);

        Assert.True(highlights.Count >= minimumExpected);
        Assert.DoesNotContain(highlights, x => x.Contains("ROI", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(highlights, x => x.Contains("caused", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Empty_review_is_explicit()
    {
        var counts = new WeeklyReviewCounts(0, 0, 0, 0, 0, 0, 0, 0, 0);
        var highlights = WeeklyReviewPolicy.Highlights(counts);

        Assert.Single(highlights);
        Assert.Contains("No recorded Action or Outcome activity", highlights[0]);
    }
}
