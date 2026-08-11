using Xunit;

namespace Atlas.Api.Tests;

public sealed class BusinessDiscoveryEndpointWiringTests
{
    [Fact]
    public void Discovery_endpoint_persists_the_full_reconciliation_result()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "apps", "api", "BusinessDiscovery.cs"));

        Assert.Contains("BusinessDiscoverySnapshot.Create(account.Id, reconciliation)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BusinessDiscoverySnapshot.Create(account.Id, publicSnapshot)", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "apps", "api", "BusinessDiscovery.cs")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Atlas repository root from the test output directory.");
    }
}
