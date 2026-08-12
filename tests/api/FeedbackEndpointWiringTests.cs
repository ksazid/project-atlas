using Xunit;

namespace Atlas.Api.Tests;

public sealed class FeedbackEndpointWiringTests
{
    [Fact]
    public void Feedback_endpoint_is_business_scoped_and_owner_authorized()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "apps", "api", "Program.cs"));
        var feedback = File.ReadAllText(Path.Combine(root, "apps", "api", "Feedback.cs"));

        Assert.Contains("app.MapFeedbackEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains("/api/v1/businesses/{businessId:guid}/feedback", feedback, StringComparison.Ordinal);
        Assert.Contains("RequireAuthorization(\"BusinessOwner\")", feedback, StringComparison.Ordinal);
        Assert.Contains("MembershipRoles.BusinessOwner", feedback, StringComparison.Ordinal);
        Assert.Contains("ProviderSubject", feedback, StringComparison.Ordinal);
        Assert.Contains("x.BusinessId == businessId", feedback, StringComparison.Ordinal);
    }

    [Fact]
    public void Feedback_endpoint_uses_stable_validation_and_safe_not_found_shapes()
    {
        var root = FindRepositoryRoot();
        var feedback = File.ReadAllText(Path.Combine(root, "apps", "api", "Feedback.cs"));

        Assert.Contains("feedback_invalid", feedback, StringComparison.Ordinal);
        Assert.Contains("Results.NotFound()", feedback, StringComparison.Ordinal);
        Assert.Contains("Results.Created", feedback, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Forbid", feedback, StringComparison.Ordinal);
    }

    [Fact]
    public void Feedback_migration_is_forward_only_and_registered_by_attribute()
    {
        var root = FindRepositoryRoot();
        var migrationPath = Path.Combine(root, "apps", "api", "Migrations", "20260812113000_FeedbackSupport.cs");
        Assert.True(File.Exists(migrationPath), "VS-32 feedback migration must exist.");
        var migration = File.ReadAllText(migrationPath);
        Assert.Contains("[Migration(\"20260812113000_FeedbackSupport\")]", migration, StringComparison.Ordinal);
        Assert.Contains("CreateTable", migration, StringComparison.Ordinal);
        Assert.Contains("FeedbackRecords", migration, StringComparison.Ordinal);
        Assert.Contains("DropTable", migration, StringComparison.Ordinal);
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