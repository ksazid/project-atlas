using Xunit;

namespace Atlas.Api.Tests;

public sealed class PilotOperationsEndpointWiringTests
{
    [Fact]
    public void Pilot_operations_routes_are_internal_operator_only()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "apps", "api", "Program.cs"));
        var pilot = File.ReadAllText(Path.Combine(root, "apps", "api", "PilotOperations.cs"));

        Assert.Contains("app.MapPilotOperationsEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains("/api/v1/pilot-operations/businesses", pilot, StringComparison.Ordinal);
        Assert.Contains("/api/v1/pilot-operations/businesses/{businessId:guid}", pilot, StringComparison.Ordinal);
        Assert.Contains("/api/v1/pilot-operations/businesses/{businessId:guid}/notes", pilot, StringComparison.Ordinal);
        Assert.Contains("/api/v1/pilot-operations/businesses/{businessId:guid}/profile", pilot, StringComparison.Ordinal);
        Assert.True(Count(pilot, "RequireAuthorization(\"InternalOperator\")") >= 4);
        Assert.DoesNotContain("RequireAuthorization(\"BusinessOwner\")", pilot, StringComparison.Ordinal);
    }

    [Fact]
    public void Pilot_operations_contract_is_review_first_without_synthetic_quality_score()
    {
        var root = FindRepositoryRoot();
        var pilot = File.ReadAllText(Path.Combine(root, "apps", "api", "PilotOperations.cs"));

        Assert.Contains("PilotBusinessListItem", pilot, StringComparison.Ordinal);
        Assert.Contains("UnsafeFeedbackCount", pilot, StringComparison.Ordinal);
        Assert.Contains("UsefulFeedbackCount", pilot, StringComparison.Ordinal);
        Assert.Contains("NotUsefulFeedbackCount", pilot, StringComparison.Ordinal);
        Assert.Contains("LatestGenerationOutcome", pilot, StringComparison.Ordinal);
        Assert.DoesNotContain("QualityScore", pilot, StringComparison.Ordinal);
        Assert.DoesNotContain("Impersonat", pilot, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "apps", "api", "Program.cs")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Atlas repository root from the test output directory.");
    }
}
