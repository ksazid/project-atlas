using System.Text.Json;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalOpportunityGenerationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("gross-sales", -0.10, "revenue", "sales-decline-review")]
    [InlineData("orders", -0.20, "growth", "order-decline-review")]
    [InlineData("repeat-orders", -0.15, "retention", "repeat-order-decline-review")]
    [InlineData("delivery-time", 0.10, "efficiency", "delivery-time-deterioration-review")]
    public void Eligible_operational_change_generates_only_matching_goal_aligned_pattern(
        string metric, double delta, string goalType, string expectedPattern)
    {
        var businessId = Guid.NewGuid();
        var change = Change(businessId, metric, (decimal)delta);
        var bundle = Bundle("restaurant-cafe", OperationalChangeEvidenceCodec.Encode(change, OperationalFreshness.Fresh));

        var result = OpportunityGenerator.Generate(Profile(businessId), [Goal(businessId, goalType)], bundle, [], Now);
        var candidate = Assert.Single(result.Candidates, item => item.PatternKey == expectedPattern);

        var operational = Assert.Single(candidate.Evidence, item => item.Layer == KnowledgeEvidenceLayers.Operational);
        Assert.Contains(change.Id.ToString("D"), operational.Source, StringComparison.Ordinal);
        Assert.All(changeSignalIds(change), id => Assert.Contains(id.ToString("D"), operational.Source, StringComparison.Ordinal));
        Assert.Contains(candidate.Limitations, text => text.Contains("does not prove", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("caused", candidate.Reason + candidate.WhyNow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Stale_eligible_operational_change_forces_low_confidence()
    {
        var businessId = Guid.NewGuid();
        var fact = OperationalChangeEvidenceCodec.Encode(Change(businessId, "gross-sales", -.20m), OperationalFreshness.Stale);

        var result = OpportunityGenerator.Generate(Profile(businessId), [Goal(businessId, "revenue")], Bundle("restaurant-cafe", fact), [], Now);

        Assert.Equal("Low", Assert.Single(result.Candidates, item => item.PatternKey == "sales-decline-review").Confidence);
    }

    [Theory]
    [InlineData("unsupported", -0.20)]
    [InlineData("gross-sales", -0.099)]
    [InlineData("gross-sales", 0.20)]
    [InlineData("gross-sales", 0.00)]
    public void Unsupported_weak_positive_or_unchanged_evidence_produces_no_operational_pattern(string metric, double delta)
    {
        var businessId = Guid.NewGuid();
        var fact = OperationalChangeEvidenceCodec.Encode(Change(businessId, metric, (decimal)delta), OperationalFreshness.Fresh);

        var result = OpportunityGenerator.Generate(Profile(businessId), [Goal(businessId, "revenue")], Bundle("restaurant-cafe", fact), [], Now);

        Assert.DoesNotContain(result.Candidates, item => item.Evidence.Any(evidence => evidence.Layer == KnowledgeEvidenceLayers.Operational));
    }

    [Fact]
    public void Generic_category_does_not_receive_restaurant_operational_pattern()
    {
        var businessId = Guid.NewGuid();
        var fact = OperationalChangeEvidenceCodec.Encode(Change(businessId, "gross-sales", -.20m), OperationalFreshness.Fresh);

        var result = OpportunityGenerator.Generate(Profile(businessId), [Goal(businessId, "revenue")], Bundle("retail", fact), [], Now);

        Assert.DoesNotContain(result.Candidates, item => item.PatternKey == "sales-decline-review");
    }

    private static BusinessProfile Profile(Guid businessId) => new()
    {
        BusinessId = businessId, Language = "en", Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = Now
    };

    private static BusinessGoal Goal(Guid businessId, string type) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, Type = type, Title = "Improve operations", Priority = 1, UpdatedAt = Now
    };

    private static ResolvedKnowledgeBundle Bundle(string category, ResolvedKnowledgeFact fact)
    {
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var manifests = new List<ResolvedKnowledgeManifest>
        {
            new(core.Layer, core.PackKey, core.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(core))
        };
        if (category == "restaurant-cafe")
        {
            var restaurant = RestaurantCafeKnowledgeManifestV2.Create();
            manifests.Add(new(restaurant.Layer, restaurant.PackKey, restaurant.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(restaurant)));
        }
        return new(category, null, manifests, [], [], [], "operational-bundle") { OperationalFacts = [fact] };
    }

    private static BusinessChange Change(Guid businessId, string metric, decimal delta)
    {
        var signals = new[] { Guid.NewGuid(), Guid.NewGuid() };
        return new()
        {
            Id = Guid.NewGuid(), BusinessId = businessId, Identity = Guid.NewGuid().ToString("N"), MetricKey = metric,
            CurrentValue = 100m + 100m * delta, ComparisonValue = 100m, AbsoluteDelta = 100m * delta, RelativeDelta = delta,
            CurrentPeriodStart = new DateOnly(2026, 8, 6), CurrentPeriodEnd = new DateOnly(2026, 8, 12),
            ComparisonPeriodStart = new DateOnly(2026, 7, 30), ComparisonPeriodEnd = new DateOnly(2026, 8, 5),
            EvidenceSignalIdsJson = JsonSerializer.Serialize(signals), ObservedAt = Now, Confidence = "high"
        };
    }

    private static Guid[] changeSignalIds(BusinessChange change) =>
        JsonSerializer.Deserialize<Guid[]>(change.EvidenceSignalIdsJson)!;
}
