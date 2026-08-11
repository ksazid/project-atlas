using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryReconciliationTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 8, 11, 1, 45, 0, TimeSpan.Zero);

    [Fact]
    public void Reconciler_PrimaryWins_SecondaryFillsMissing_AndThirdFillsRemainingGap()
    {
        var result = BusinessDiscoveryReconciler.Reconcile([
            Source(0, true, "website", "https://antalya.example", Facts(
                ("name", "Antalya Kebab St. Julian's"),
                ("phone", "+356 2100 0000"))),
            Source(1, false, "bolt-food", "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians", Facts(
                ("name", "Antalya Kebab St. Julian's"),
                ("phone", "+356 2100 0000"),
                ("description", "Turkish kebab restaurant"))),
            Source(2, false, "wolt", "https://wolt.com/en/mlt/malta/restaurant/antalya-kebab", Facts(
                ("name", "Antalya Kebab St. Julian's"),
                ("openingHours", "Mo-Su 11:00-23:00")))
        ]);

        Assert.Equal("Antalya Kebab St. Julian's", Fact(result, "name"));
        Assert.Equal("+356 2100 0000", Fact(result, "phone"));
        Assert.Equal("Turkish kebab restaurant", Fact(result, "description"));
        Assert.Equal("Mo-Su 11:00-23:00", Fact(result, "openingHours"));
        Assert.Equal(4, result.Snapshot.Facts.Count);

        Assert.Contains(result.Evidence, x => x.SourceOrder == 0 && x.Key == "name" && x.ReconciliationState == "selected");
        Assert.Contains(result.Evidence, x => x.SourceOrder == 1 && x.Key == "name" && x.ReconciliationState == "corroborating");
        Assert.Contains(result.Evidence, x => x.SourceOrder == 1 && x.Key == "phone" && x.ReconciliationState == "corroborating");
        Assert.Contains(result.Evidence, x => x.SourceOrder == 1 && x.Key == "description" && x.ReconciliationState == "selected");
        Assert.Contains(result.Evidence, x => x.SourceOrder == 2 && x.Key == "openingHours" && x.ReconciliationState == "selected");
    }

    [Fact]
    public void Reconciler_ConflictNeverOverwritesHigherPriorityValue()
    {
        var result = BusinessDiscoveryReconciler.Reconcile([
            Source(0, true, "website", "https://antalya.example", Facts(
                ("name", "Antalya Kebab St. Julian's"),
                ("phone", "+356 2100 0000"))),
            Source(1, false, "wolt", "https://wolt.com/en/mlt/malta/restaurant/antalya-kebab", Facts(
                ("name", "Antalya Kebab St. Julian's"),
                ("phone", "+356 7999 9999")))
        ]);

        Assert.Equal("+356 2100 0000", Fact(result, "phone"));
        Assert.Contains(result.Evidence, x => x.SourceOrder == 1 && x.Key == "phone" && x.ReconciliationState == "conflict");
        Assert.Contains("business_source_conflict", result.Warnings);
    }

    [Fact]
    public void Reconciler_ClearBusinessMismatchCannotContaminateSelectedFacts()
    {
        var result = BusinessDiscoveryReconciler.Reconcile([
            Source(0, true, "website", "https://antalya.example", Facts(
                ("name", "Antalya Kebab St. Julian's"),
                ("phone", "+356 2100 0000"))),
            Source(1, false, "website", "https://unrelated.example", Facts(
                ("name", "Completely Different Florist"),
                ("description", "Flowers and gifts")))
        ]);

        Assert.DoesNotContain(result.Snapshot.Facts, x => x.Value == "Flowers and gifts");
        Assert.Contains(result.Evidence, x => x.SourceOrder == 1 && x.Key == "description" && x.ReconciliationState == "excluded");
        Assert.Contains(result.SourceResults, x => x.Order == 1 && x.AssociationStatus == "mismatch");
        Assert.Contains("business_source_identity_mismatch", result.Warnings);
    }

    [Fact]
    public void Reconciler_PrimaryFailureFallsBackToFirstUsableSecondaryAnchor()
    {
        var result = BusinessDiscoveryReconciler.Reconcile([
            Source(0, true, "website", "https://down.example", [], status: "unavailable", warning: "business_source_unavailable"),
            Source(1, false, "bolt-food", "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians", Facts(
                ("name", "Antalya Kebab St. Julian's"),
                ("description", "Turkish kebab restaurant"))),
            Source(2, false, "wolt", "https://wolt.com/en/mlt/malta/restaurant/antalya-kebab", Facts(
                ("name", "Antalya Kebab St. Julian's"),
                ("openingHours", "Mo-Su 11:00-23:00")))
        ]);

        Assert.Equal("bolt-food", result.Snapshot.Provider);
        Assert.Equal("Antalya Kebab St. Julian's", Fact(result, "name"));
        Assert.Equal("Mo-Su 11:00-23:00", Fact(result, "openingHours"));
        Assert.Contains(result.SourceResults, x => x.Order == 0 && x.Status == "unavailable");
        Assert.Contains(result.SourceResults, x => x.Order == 1 && x.AssociationStatus == "anchor");
        Assert.Contains("business_source_unavailable", result.Warnings);
    }

    [Fact]
    public void Reconciler_ThrowsStableErrorWhenNoSourceProvidesUsefulFacts()
    {
        var error = Assert.Throws<BusinessDiscoveryException>(() => BusinessDiscoveryReconciler.Reconcile([
            Source(0, true, "website", "https://down.example", [], status: "unavailable", warning: "business_source_unavailable"),
            Source(1, false, "wolt", "https://wolt.com/en/mlt/malta/restaurant/empty", [], status: "no-facts", warning: "business_source_no_facts")
        ]));

        Assert.Equal("business_sources_no_facts", error.Code);
    }

    private static string Fact(BusinessDiscoveryReconciliationResult result, string key) =>
        result.Snapshot.Facts.Single(x => x.Key == key).Value;

    private static BusinessSourceObservation Source(
        int order,
        bool primary,
        string provider,
        string url,
        IReadOnlyList<PublicBusinessFact> facts,
        string status = "success",
        string? warning = null) =>
        new(order, primary, provider, url, status, facts, warning);

    private static IReadOnlyList<PublicBusinessFact> Facts(params (string Key, string Value)[] values) =>
        values.Select(value => new PublicBusinessFact(
            value.Key,
            value.Value,
            "public",
            "https://source.example",
            ObservedAt,
            "high")).ToList();
}
