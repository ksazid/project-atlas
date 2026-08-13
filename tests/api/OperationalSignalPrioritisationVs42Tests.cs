using System.Text.Json;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalSignalPrioritisationVs42Tests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Fresh_material_operational_signal_outranks_generic_review_for_same_priority_goal()
    {
        var businessId = Guid.NewGuid();
        var operational = OperationalChangeEvidenceCodec.Encode(Change(businessId, "gross-sales", -.20m), OperationalFreshness.Fresh);
        var bundle = Bundle(businessId,
            context: [new("context", "operatingchannels", "Dine in | Takeaway | Delivery", FieldSources.Owner)],
            operational: [operational]);

        var result = OpportunityGenerator.Generate(Profile(businessId), [Goal(businessId, "revenue")], bundle, [], Now);

        Assert.Contains(result.Candidates, candidate => candidate.PatternKey == "ordering-path-clarity-review");
        Assert.Contains(result.Candidates, candidate => candidate.PatternKey == "sales-decline-review");
        Assert.Equal("sales-decline-review", result.Selected?.PatternKey);
        Assert.Contains(result.Selected!.Evidence, evidence => evidence.Layer == KnowledgeEvidenceLayers.Operational);
    }

    [Fact]
    public void Larger_fresh_operational_movement_wins_when_goal_priority_is_equal()
    {
        var businessId = Guid.NewGuid();
        var sales = OperationalChangeEvidenceCodec.Encode(Change(businessId, "gross-sales", -.15m), OperationalFreshness.Fresh);
        var orders = OperationalChangeEvidenceCodec.Encode(Change(businessId, "orders", -.30m), OperationalFreshness.Fresh);
        var bundle = Bundle(businessId, operational: [sales, orders]);

        var result = OpportunityGenerator.Generate(Profile(businessId), [Goal(businessId, "revenue")], bundle, [], Now);

        Assert.Contains(result.Candidates, candidate => candidate.PatternKey == "sales-decline-review");
        Assert.Contains(result.Candidates, candidate => candidate.PatternKey == "order-decline-review");
        Assert.Equal("order-decline-review", result.Selected?.PatternKey);
    }

    [Fact]
    public void Stale_operational_signal_does_not_displace_supported_generic_review()
    {
        var businessId = Guid.NewGuid();
        var operational = OperationalChangeEvidenceCodec.Encode(Change(businessId, "gross-sales", -.20m), OperationalFreshness.Stale);
        var bundle = Bundle(businessId,
            context: [new("context", "operatingchannels", "Takeaway", FieldSources.Owner)],
            operational: [operational]);

        var result = OpportunityGenerator.Generate(Profile(businessId), [Goal(businessId, "revenue")], bundle, [], Now);

        Assert.Contains(result.Candidates, candidate => candidate.PatternKey == "sales-decline-review");
        Assert.Equal("ordering-path-clarity-review", result.Selected?.PatternKey);
    }

    private static BusinessProfile Profile(Guid businessId) => new()
    {
        BusinessId = businessId, Language = "en", Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = Now
    };

    private static BusinessGoal Goal(Guid businessId, string type) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, Type = type, Title = "Improve trading performance", Priority = 1, UpdatedAt = Now
    };

    private static ResolvedKnowledgeBundle Bundle(
        Guid businessId,
        IReadOnlyList<ResolvedKnowledgeFact>? context = null,
        IReadOnlyList<ResolvedKnowledgeFact>? operational = null)
    {
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var restaurant = RestaurantCafeKnowledgeManifestV2.Create();
        var manifests = new List<ResolvedKnowledgeManifest>
        {
            new(core.Layer, core.PackKey, core.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(core)),
            new(restaurant.Layer, restaurant.PackKey, restaurant.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(restaurant))
        };
        return new("restaurant-cafe", null, manifests, context ?? [], [], [], $"vs42-{businessId:N}")
        {
            OperationalFacts = operational ?? []
        };
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
}
