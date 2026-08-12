using Xunit;

namespace Atlas.Api.Tests;

public sealed class PilotOpportunityOperationEndpointWiringTests
{
    [Fact]
    public void Preparation_and_withdrawal_routes_are_internal_operator_only()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "apps", "api", "Program.cs"));
        var sourcePath = Path.Combine(root, "apps", "api", "PilotOpportunityOperationsEndpoints.cs");

        Assert.True(File.Exists(sourcePath));
        var source = File.ReadAllText(sourcePath);
        Assert.Contains("app.MapPilotOpportunityOperationsEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains("/api/v1/pilot-operations/businesses/{businessId:guid}/opportunity-candidate", source, StringComparison.Ordinal);
        Assert.Contains("/api/v1/pilot-operations/businesses/{businessId:guid}/opportunities", source, StringComparison.Ordinal);
        Assert.Contains("/api/v1/pilot-operations/businesses/{businessId:guid}/opportunities/{opportunityId:guid}/withdraw", source, StringComparison.Ordinal);
        Assert.True(Count(source, "RequireAuthorization(\"InternalOperator\")") >= 3);
        Assert.DoesNotContain("RequireAuthorization(\"BusinessOwner\")", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Operator_routes_do_not_accept_free_form_recommendation_copy()
    {
        var root = FindRepositoryRoot();
        var preparation = File.ReadAllText(Path.Combine(root, "apps", "api", "PilotOpportunityPreparation.cs"));
        Assert.Contains("PilotPrepareOpportunityRequest", preparation, StringComparison.Ordinal);
        Assert.DoesNotContain("PilotPrepareOpportunityRequest(\n    string Title", preparation, StringComparison.Ordinal);
        Assert.DoesNotContain("string WhyItMatters", preparation, StringComparison.Ordinal);
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
