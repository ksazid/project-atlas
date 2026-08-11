using Xunit;

namespace Atlas.Api.Tests;

public sealed class DevelopmentResetPolicyTests
{
    [Fact]
    public void Expo_demo_reset_is_development_only_and_keeps_the_demo_account()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "apps", "api", "BusinessHub.cs"));

        Assert.Contains("app.Environment.IsDevelopment()", source, StringComparison.Ordinal);
        Assert.Contains("/api/v1/dev/reset-business", source, StringComparison.Ordinal);
        Assert.Contains("atlas-expo-go-demo-owner", source, StringComparison.Ordinal);
        Assert.Contains("DELETE FROM \\\"BusinessMemberships\\\"", source, StringComparison.Ordinal);
        Assert.Contains("DELETE FROM \\\"Businesses\\\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM \\\"UserAccounts\\\"", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "apps", "api", "BusinessHub.cs")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Atlas repository root from the test output directory.");
    }
}
