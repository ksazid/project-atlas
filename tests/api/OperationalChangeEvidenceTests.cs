using System.Text.Json;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalChangeEvidenceTests
{
    private static readonly Guid FirstSignalId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondSignalId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    [Fact]
    public void Codec_round_trips_material_observed_change_with_exact_provenance()
    {
        var change = Change();

        var fact = OperationalChangeEvidenceCodec.Encode(change, OperationalFreshness.Fresh);
        var parsed = OperationalChangeEvidenceCodec.TryParse(fact, out var evidence);

        Assert.True(parsed);
        Assert.NotNull(evidence);
        Assert.Equal(change.Id, evidence.ChangeId);
        Assert.Equal("gross-sales", evidence.MetricKey);
        Assert.Equal(7, evidence.WindowDays);
        Assert.Equal(90m, evidence.CurrentValue);
        Assert.Equal(100m, evidence.ComparisonValue);
        Assert.Equal(-10m, evidence.AbsoluteDelta);
        Assert.Equal(-.10m, evidence.RelativeDelta);
        Assert.Equal(OperationalFreshness.Fresh, evidence.Freshness);
        Assert.Equal("high", evidence.Confidence);
        Assert.Equal(new[] { FirstSignalId, SecondSignalId }, evidence.SignalIds);
        Assert.Contains("observed", fact.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("caused", fact.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("context", "gross-sales-change-7d", "{}", "operational-change:bad")]
    [InlineData("operational", "gross-sales-change-7d", "not-json", "operational-change:bad")]
    [InlineData("operational", "gross-sales-change-7d", "{}", "owner")]
    public void Invalid_fact_value_or_source_is_rejected(string layer, string key, string value, string source)
    {
        Assert.False(OperationalChangeEvidenceCodec.TryParse(new(layer, key, value, source), out _));
    }

    [Fact]
    public void Missing_signal_provenance_is_rejected()
    {
        var change = Change();
        change.EvidenceSignalIdsJson = "[]";
        var fact = OperationalChangeEvidenceCodec.Encode(change, OperationalFreshness.Fresh);

        Assert.False(OperationalChangeEvidenceCodec.TryParse(fact, out _));
    }

    [Fact]
    public void Zero_comparison_is_rejected()
    {
        var change = Change();
        change.ComparisonValue = 0m;
        var fact = OperationalChangeEvidenceCodec.Encode(change, OperationalFreshness.Fresh);

        Assert.False(OperationalChangeEvidenceCodec.TryParse(fact, out _));
    }

    [Fact]
    public void Unsupported_window_is_rejected()
    {
        var change = Change();
        change.CurrentPeriodStart = new DateOnly(2026, 8, 1);
        change.CurrentPeriodEnd = new DateOnly(2026, 8, 12);
        var fact = OperationalChangeEvidenceCodec.Encode(change, OperationalFreshness.Fresh);

        Assert.False(OperationalChangeEvidenceCodec.TryParse(fact, out _));
    }

    private static BusinessChange Change() => new()
    {
        Id = Guid.Parse("20000000-0000-0000-0000-000000000039"),
        BusinessId = Guid.Parse("30000000-0000-0000-0000-000000000039"),
        Identity = "change-39",
        MetricKey = "gross-sales",
        CurrentValue = 90m,
        ComparisonValue = 100m,
        AbsoluteDelta = -10m,
        RelativeDelta = -.10m,
        CurrentPeriodStart = new DateOnly(2026, 8, 6),
        CurrentPeriodEnd = new DateOnly(2026, 8, 12),
        ComparisonPeriodStart = new DateOnly(2026, 7, 30),
        ComparisonPeriodEnd = new DateOnly(2026, 8, 5),
        EvidenceSignalIdsJson = JsonSerializer.Serialize(new[] { SecondSignalId, FirstSignalId }),
        ObservedAt = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero),
        Confidence = "high"
    };
}
