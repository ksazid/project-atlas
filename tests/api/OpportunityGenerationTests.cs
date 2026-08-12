using System.Text.Json;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OpportunityGenerationTests
{
    [Fact]
    public void Restaurant_ordering_channel_generates_category_specific_candidate()
    {
        var businessId = Guid.NewGuid();
        var goal = Goal(businessId, "growth", "Grow direct orders", 1);
        var bundle = Bundle("restaurant-cafe",
            context: [Fact("context", "primarychannels", "Takeaway", "owner")]);

        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], bundle, [], Now);

        Assert.NotNull(result.Selected);
        Assert.Equal("ordering-path-clarity-review", result.Selected!.PatternKey);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.PackKey, result.Selected.KnowledgePackKey);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.Version, result.Selected.KnowledgePackVersion);
        Assert.Equal(bundle.Fingerprint, result.Selected.BundleFingerprint);
        Assert.Equal(goal.Id, result.Selected.GoalId);
        Assert.Contains(result.Selected.Evidence, x => x.Key == "primarychannels" && x.Value == "Takeaway");
    }

    [Fact]
    public void Restaurant_confirmed_operating_channels_generate_category_specific_candidate()
    {
        var businessId = Guid.NewGuid();
        var goal = Goal(businessId, "revenue", "Increase revenue", 1);
        var bundle = Bundle("restaurant-cafe",
            context: [Fact("context", "operatingchannels", "Dine in | Takeaway | Delivery", "owner")]);

        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], bundle, [], Now);
        var focusCandidate = OpportunityFocusService.SelectEligibleCandidate(result);

        Assert.NotNull(result.Selected);
        Assert.Equal("ordering-path-clarity-review", result.Selected!.PatternKey);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.PackKey, result.Selected.KnowledgePackKey);
        Assert.Contains(result.Selected.Evidence, x =>
            x.Key == "operatingchannels" && x.Value == "Dine in | Takeaway | Delivery");
        Assert.NotNull(focusCandidate);
        Assert.Equal("ordering-path-clarity-review", focusCandidate!.PatternKey);
    }

    [Fact]
    public void Missing_ordering_channel_blocks_ordering_pattern()
    {
        var businessId = Guid.NewGuid();
        var bundle = Bundle("restaurant-cafe");

        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [Goal(businessId, "growth", "Grow orders", 1)], bundle, [], Now);

        Assert.DoesNotContain(result.Candidates, x => x.PatternKey == "ordering-path-clarity-review");
    }

    [Fact]
    public void Hours_pattern_requires_explicit_hours_evidence()
    {
        var businessId = Guid.NewGuid();
        var goal = Goal(businessId, "customer-experience", "Reduce customer friction", 1);

        var withoutHours = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], Bundle("restaurant-cafe"), [], Now);
        var withHours = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], Bundle("restaurant-cafe",
            context: [Fact("context", "businessHours", "Mon-Sat 10:00-22:00", "owner")]), [], Now);

        Assert.DoesNotContain(withoutHours.Candidates, x => x.PatternKey == "hours-consistency-review");
        Assert.Contains(withHours.Candidates, x => x.PatternKey == "hours-consistency-review");
    }

    [Fact]
    public void Offer_pattern_requires_owner_confirmed_context_not_unconfirmed_memory()
    {
        var businessId = Guid.NewGuid();
        var goal = Goal(businessId, "growth", "Increase demand", 1);
        var confirmedBundle = Bundle("restaurant-cafe", context: [Fact("context", "currentpriorities", "Promote weekday lunch", "owner")]);
        var memoryOnlyBundle = Bundle("restaurant-cafe", memory: [Fact("memory", "currentpriorities", "Promote weekday lunch", "public")]);

        var confirmedResult = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], confirmedBundle, [], Now);
        var memoryOnlyResult = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], memoryOnlyBundle, [], Now);

        Assert.Contains(confirmedResult.Candidates, x => x.PatternKey == "current-offer-visibility-review");
        Assert.DoesNotContain(memoryOnlyResult.Candidates, x => x.PatternKey == "current-offer-visibility-review");
    }

    [Fact]
    public void Reputation_pattern_accepts_attributable_public_signal_and_reduces_confidence()
    {
        var businessId = Guid.NewGuid();
        var goal = Goal(businessId, "retention", "Improve repeat visits", 1);
        var bundle = Bundle("restaurant-cafe", memory: [Fact("memory", "reviewSignal", "Recent reviews mention slow pickup", "public")]);

        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], bundle, [], Now);
        var candidate = Assert.Single(result.Candidates, x => x.PatternKey == "reputation-signal-follow-up");

        Assert.Equal("Low", candidate.Confidence);
        Assert.Contains(candidate.Evidence, x => x.Key == "reviewSignal" && x.Source == "public");
    }

    [Fact]
    public void Unsupported_category_never_receives_restaurant_patterns()
    {
        var businessId = Guid.NewGuid();
        var bundle = Bundle("retail", includeRestaurantManifest: false,
            context: [Fact("context", "primarychannels", "Own website/app", "owner"), Fact("context", "businessHours", "09:00-18:00", "owner")]);

        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [Goal(businessId, "growth", "Grow sales", 1)], bundle, [], Now);

        Assert.DoesNotContain(result.Candidates, x => x.KnowledgePackKey == RestaurantCafeKnowledgeManifestV2.PackKey);
        Assert.DoesNotContain(result.Candidates, x => x.PatternKey.Contains("ordering", StringComparison.Ordinal));
    }

    [Fact]
    public void Pattern_without_matching_goal_is_not_generated()
    {
        var businessId = Guid.NewGuid();
        var bundle = Bundle("restaurant-cafe", context: [Fact("context", "primarychannels", "Marketplace/platform", "owner")]);

        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [Goal(businessId, "reputation", "Improve reviews", 1)], bundle, [], Now);

        Assert.DoesNotContain(result.Candidates, x => x.PatternKey == "ordering-path-clarity-review");
    }

    [Fact]
    public void No_eligible_pattern_returns_no_selected_candidate_instead_of_filler()
    {
        var businessId = Guid.NewGuid();
        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [Goal(businessId, "reputation", "Improve reviews", 1)], Bundle("restaurant-cafe"), [], Now);

        Assert.Null(result.Selected);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void Policy_only_profile_and_goal_do_not_qualify_for_todays_focus()
    {
        var businessId = Guid.NewGuid();
        var result = OpportunityGenerator.Generate(
            ConfirmedProfile(businessId),
            [Goal(businessId, "revenue", "Increase revenue", 1)],
            Bundle("restaurant-cafe"),
            [],
            Now);

        Assert.NotNull(result.Selected);
        Assert.Empty(result.Selected!.Evidence.Where(x => x.Layer != "policy"));
        Assert.Null(OpportunityFocusService.SelectEligibleCandidate(result));
    }

    [Fact]
    public void Evidence_ids_are_stable_and_reference_only_supplied_bundle_facts()
    {
        var businessId = Guid.NewGuid();
        var evidence = Fact("context", "primarychannels", "Takeaway", "owner");
        var bundle = Bundle("restaurant-cafe", context: [evidence]);
        var goal = Goal(businessId, "growth", "Grow orders", 1);

        var first = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], bundle, [], Now);
        var second = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], bundle, [], Now);

        var item = Assert.Single(first.Selected!.Evidence, x => x.Key == "primarychannels");
        var repeatedItem = Assert.Single(second.Selected!.Evidence, x => x.Key == "primarychannels");
        Assert.False(string.IsNullOrWhiteSpace(item.EvidenceId));
        Assert.Equal(item.EvidenceId, repeatedItem.EvidenceId);
        Assert.All(first.Selected.Evidence.Where(x => x.Layer != "policy"), x =>
            Assert.Contains(bundle.ContextFacts.Concat(bundle.LocalMarketFacts).Concat(bundle.MemoryFacts), f => f.Layer == x.Layer && f.Key == x.Key && f.Value == x.Value && f.Source == x.Source));
    }

    [Fact]
    public void Cooldown_suppresses_same_pattern_but_not_legacy_opportunity()
    {
        var businessId = Guid.NewGuid();
        var bundle = Bundle("restaurant-cafe", context: [Fact("context", "primarychannels", "Marketplace/platform", "owner")]);
        var goal = Goal(businessId, "growth", "Grow orders", 1);
        var previous = PriorOpportunity(businessId, "ordering-path-clarity-review", Now.AddDays(-2));
        var legacy = PriorOpportunity(businessId, null, Now.AddDays(-1));

        var suppressed = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], bundle, [previous], Now);
        var allowed = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], bundle, [legacy], Now);

        Assert.DoesNotContain(suppressed.Candidates, x => x.PatternKey == "ordering-path-clarity-review");
        Assert.Contains(allowed.Candidates, x => x.PatternKey == "ordering-path-clarity-review");
    }

    [Fact]
    public void Ranking_prefers_highest_priority_goal_then_category_specific_candidate()
    {
        var businessId = Guid.NewGuid();
        var bundle = Bundle("restaurant-cafe", context:
        [
            Fact("context", "primarychannels", "Marketplace/platform", "owner"),
            Fact("context", "currentpriorities", "Promote weekday lunch", "owner")
        ]);
        var goals = new[]
        {
            Goal(businessId, "retention", "Retain customers", 2),
            Goal(businessId, "growth", "Grow orders", 1)
        };

        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), goals, bundle, [], Now);

        Assert.NotNull(result.Selected);
        Assert.Equal(1, result.Selected!.GoalPriority);
        Assert.Equal(RestaurantCafeKnowledgeManifestV2.PackKey, result.Selected.KnowledgePackKey);
    }

    [Fact]
    public void Snapshot_separates_evidence_from_interpretation_and_retains_exact_versions()
    {
        var businessId = Guid.NewGuid();
        var bundle = Bundle("restaurant-cafe", context: [Fact("context", "primarychannels", "Marketplace/platform", "owner")]);
        var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [Goal(businessId, "growth", "Grow orders", 1)], bundle, [], Now);

        var snapshotJson = OpportunityGenerationSnapshot.Serialize(result.Selected!);
        using var document = JsonDocument.Parse(snapshotJson);
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("ordering-path-clarity-review", root.GetProperty("patternKey").GetString());
        Assert.Equal(bundle.Fingerprint, root.GetProperty("bundleFingerprint").GetString());
        Assert.True(root.GetProperty("evidence").GetArrayLength() > 0);
        Assert.True(root.GetProperty("assumptions").GetArrayLength() > 0);
        Assert.True(root.GetProperty("limitations").GetArrayLength() > 0);
        Assert.DoesNotContain("whyNow", root.GetProperty("evidence").ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(root.GetProperty("manifests").EnumerateArray(), x => x.GetProperty("packKey").GetString() == RestaurantCafeKnowledgeManifestV2.PackKey);
    }

    private static readonly DateTimeOffset Now = new(2026, 8, 11, 1, 0, 0, TimeSpan.Zero);

    private static BusinessProfile ConfirmedProfile(Guid businessId) => new()
    {
        BusinessId = businessId,
        Language = "en",
        Source = FieldSources.Owner,
        OwnerConfirmed = true,
        UpdatedAt = Now
    };

    private static BusinessGoal Goal(Guid businessId, string type, string title, int priority) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, Type = type, Title = title, Priority = priority, UpdatedAt = Now
    };

    private static ResolvedKnowledgeFact Fact(string layer, string key, string value, string source) => new(layer, key, value, source);

    private static ResolvedKnowledgeBundle Bundle(string category, bool includeRestaurantManifest = true,
        IReadOnlyList<ResolvedKnowledgeFact>? context = null,
        IReadOnlyList<ResolvedKnowledgeFact>? local = null,
        IReadOnlyList<ResolvedKnowledgeFact>? memory = null)
    {
        var core = GenericBusinessKnowledgeManifestV2.Create();
        var manifests = new List<ResolvedKnowledgeManifest>
        {
            new(core.Layer, core.PackKey, core.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(core))
        };
        if (includeRestaurantManifest && category == "restaurant-cafe")
        {
            var restaurant = RestaurantCafeKnowledgeManifestV2.Create();
            manifests.Add(new(restaurant.Layer, restaurant.PackKey, restaurant.ExactVersion, KnowledgePackManifestV2Policy.Fingerprint(restaurant)));
        }
        return new ResolvedKnowledgeBundle(category, null, manifests, context ?? [], local ?? [], memory ?? [], "bundle-fingerprint-123");
    }

    private static Opportunity PriorOpportunity(Guid businessId, string? patternKey, DateTimeOffset createdAt)
    {
        var evidence = patternKey is null ? "{}" : JsonSerializer.Serialize(new { schemaVersion = 1, patternKey });
        return new Opportunity
        {
            Id = Guid.NewGuid(), BusinessId = businessId, Title = "Prior", WhyItMatters = "Prior", WhyNow = "Prior",
            ExpectedImpact = "Prior", Effort = "Low", Confidence = "Medium", EvidenceSummary = "Prior", EvidenceJson = evidence,
            Status = OpportunityStatuses.Available, KnowledgePackKey = GenericBusinessKnowledgeManifestV2.Create().PackKey,
            KnowledgePackVersion = GenericBusinessKnowledgeManifestV2.Create().ExactVersion, KnowledgePackVersionId = Guid.NewGuid(),
            CreatedAt = createdAt, ExpiresAt = createdAt.AddDays(1)
        };
    }
}
