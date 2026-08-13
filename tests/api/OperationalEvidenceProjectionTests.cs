using System.Text.Json;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalEvidenceProjectionTests
{
    private static readonly Guid BusinessId = Guid.Parse("10000000-0000-0000-0000-000000000038");
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Fresh_signals_project_provider_neutral_facts_with_signal_id_provenance()
    {
        var signal = Signal(new DateOnly(2026, 8, 12), 125.50m, "high");

        var facts = OperationalEvidenceProjector.Project([signal], [], Now);

        var fact = Assert.Single(facts);
        Assert.Equal(KnowledgeEvidenceLayers.Operational, fact.Layer);
        Assert.Equal("gross-sales", fact.Key);
        Assert.Contains("125.5", fact.Value, StringComparison.Ordinal);
        Assert.Contains(signal.Id.ToString("D"), fact.Source, StringComparison.Ordinal);
        Assert.Contains("fresh", fact.Source, StringComparison.Ordinal);
    }

    [Fact]
    public void Stale_signal_is_explicit_and_historical_signal_is_excluded()
    {
        var stale = Signal(new DateOnly(2026, 7, 20), 100m, "medium");
        var historical = Signal(new DateOnly(2026, 7, 1), 90m, "low");

        var facts = OperationalEvidenceProjector.Project([stale, historical], [], Now);

        var fact = Assert.Single(facts);
        Assert.Contains(stale.Id.ToString("D"), fact.Source, StringComparison.Ordinal);
        Assert.Contains("stale", fact.Source, StringComparison.Ordinal);
        Assert.DoesNotContain(historical.Id.ToString("D"), JsonSerializer.Serialize(facts), StringComparison.Ordinal);
    }

    [Fact]
    public void Changes_preserve_underlying_signal_ids_and_observed_language()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var change = new BusinessChange
        {
            Id = Guid.NewGuid(), BusinessId = BusinessId, Identity = "change-1", MetricKey = "gross-sales",
            CurrentValue = 140m, ComparisonValue = 100m, AbsoluteDelta = 40m, RelativeDelta = .4m,
            CurrentPeriodStart = new DateOnly(2026, 8, 6), CurrentPeriodEnd = new DateOnly(2026, 8, 12),
            ComparisonPeriodStart = new DateOnly(2026, 7, 30), ComparisonPeriodEnd = new DateOnly(2026, 8, 5),
            EvidenceSignalIdsJson = JsonSerializer.Serialize(new[] { first, second }), ObservedAt = Now, Confidence = "high"
        };

        var fact = Assert.Single(OperationalEvidenceProjector.Project([], [change], Now));

        Assert.Equal("gross-sales-change-7d", fact.Key);
        Assert.Contains("observed", fact.Value, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(first.ToString("D"), fact.Source, StringComparison.Ordinal);
        Assert.Contains(second.ToString("D"), fact.Source, StringComparison.Ordinal);
        Assert.DoesNotContain("caused", fact.Value, StringComparison.OrdinalIgnoreCase);
        Assert.True(OperationalChangeEvidenceCodec.TryParse(fact, out var evidence));
        Assert.Equal(change.Id, evidence!.ChangeId);
        Assert.Equal(new[] { first, second }.Order().ToArray(), evidence.SignalIds);
    }

    [Fact]
    public void Bundle_fingerprint_changes_when_eligible_operational_evidence_changes()
    {
        var business = Business.Create(new("Atlas Cafe", "restaurant-cafe", "MT", "Europe/Malta", "EUR", "Valletta", "open"));
        var assignment = CoreAssignment(business.Id);
        var first = KnowledgeBundleResolver.Resolve(business, assignment, [], [], [],
            OperationalEvidenceProjector.Project([Signal(new DateOnly(2026, 8, 12), 100m, "high")], [], Now));
        var second = KnowledgeBundleResolver.Resolve(business, assignment, [], [], [],
            OperationalEvidenceProjector.Project([Signal(new DateOnly(2026, 8, 12), 120m, "high")], [], Now));

        Assert.NotEqual(first.Fingerprint, second.Fingerprint);
        Assert.Single(first.OperationalFacts);
    }

    private static BusinessSignal Signal(DateOnly date, decimal value, string confidence) => new()
    {
        Id = Guid.NewGuid(), BusinessId = BusinessId, OperationalImportId = Guid.NewGuid(), Identity = Guid.NewGuid().ToString("N"),
        MetricKey = "gross-sales", Value = value, Unit = "currency", Currency = "EUR", PeriodStart = date, PeriodEnd = date,
        SourceKind = "google-drive", SourceReference = "file-1", ObservedAt = Now, Confidence = confidence
    };

    private static BusinessKnowledgeAssignment CoreAssignment(Guid businessId)
    {
        var core = GenericBusinessKnowledgeManifestV2.Create();
        return new()
        {
            Id = Guid.NewGuid(), BusinessId = businessId, KnowledgePackId = Guid.NewGuid(), KnowledgePackVersionId = Guid.NewGuid(),
            PackKey = core.PackKey, ExactVersion = core.ExactVersion, IsCurrent = true,
            AssignedByUserAccountId = Guid.NewGuid(), AssignedAt = Now, EffectiveAt = Now
        };
    }
}
