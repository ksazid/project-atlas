using System.Text.Json;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalEvidenceMatcherTests
{
    [Theory]
    [InlineData("gross-sales", "decrease", -0.10, 7, "fresh", true)]
    [InlineData("orders", "decrease", -0.20, 28, "stale", true)]
    [InlineData("repeat-orders", "decrease", -0.0999, 7, "fresh", false)]
    [InlineData("delivery-time", "increase", 0.10, 28, "fresh", true)]
    [InlineData("delivery-time", "increase", -0.20, 28, "fresh", false)]
    [InlineData("gross-sales", "decrease", 0.20, 7, "fresh", false)]
    public void Matcher_applies_metric_direction_threshold_window_and_freshness(
        string metric, string direction, double relativeDelta, int window, string freshness, bool expected)
    {
        var requirement = new KnowledgeOperationalEvidenceRequirement(
            metric, direction, .10m, [7, 28], [OperationalFreshness.Fresh, OperationalFreshness.Stale]);
        var fact = Fact(metric, (decimal)relativeDelta, window, freshness);

        Assert.Equal(expected, OperationalEvidenceMatcher.Match(requirement, [fact]).Count == 1);
    }

    [Fact]
    public void Matcher_excludes_malformed_historical_and_zero_comparison_facts()
    {
        var requirement = new KnowledgeOperationalEvidenceRequirement(
            "gross-sales", OperationalChangeDirections.Decrease, .10m, [7, 28],
            [OperationalFreshness.Fresh, OperationalFreshness.Stale]);
        var zero = Change("gross-sales", -.20m, 7);
        zero.ComparisonValue = 0;

        var matches = OperationalEvidenceMatcher.Match(requirement,
        [
            new ResolvedKnowledgeFact("operational", "gross-sales-change-7d", "bad", "operational-change:bad"),
            OperationalChangeEvidenceCodec.Encode(Change("gross-sales", -.20m, 7), OperationalFreshness.Historical),
            OperationalChangeEvidenceCodec.Encode(zero, OperationalFreshness.Fresh)
        ]);

        Assert.Empty(matches);
    }

    [Fact]
    public void Matcher_returns_original_facts_in_deterministic_order()
    {
        var requirement = new KnowledgeOperationalEvidenceRequirement(
            "gross-sales", OperationalChangeDirections.Decrease, .10m, [7, 28], [OperationalFreshness.Fresh]);
        var first = Fact("gross-sales", -.20m, 7, OperationalFreshness.Fresh);
        var second = Fact("gross-sales", -.15m, 28, OperationalFreshness.Fresh);

        var matches = OperationalEvidenceMatcher.Match(requirement, [second, first]);

        Assert.Equal(matches.OrderBy(item => item.Key).ThenBy(item => item.Value).ThenBy(item => item.Source), matches);
        Assert.All(matches, item => Assert.Contains(item, new[] { first, second }));
    }

    private static ResolvedKnowledgeFact Fact(string metric, decimal delta, int window, string freshness) =>
        OperationalChangeEvidenceCodec.Encode(Change(metric, delta, window), freshness);

    private static BusinessChange Change(string metric, decimal relativeDelta, int window)
    {
        var end = new DateOnly(2026, 8, 12);
        return new()
        {
            Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), Identity = Guid.NewGuid().ToString("N"), MetricKey = metric,
            CurrentValue = 100m + (100m * relativeDelta), ComparisonValue = 100m,
            AbsoluteDelta = 100m * relativeDelta, RelativeDelta = relativeDelta,
            CurrentPeriodStart = end.AddDays(-(window - 1)), CurrentPeriodEnd = end,
            ComparisonPeriodStart = end.AddDays(-(window * 2 - 1)), ComparisonPeriodEnd = end.AddDays(-window),
            EvidenceSignalIdsJson = JsonSerializer.Serialize(new[] { Guid.NewGuid() }),
            ObservedAt = DateTimeOffset.UtcNow, Confidence = "high"
        };
    }
}
