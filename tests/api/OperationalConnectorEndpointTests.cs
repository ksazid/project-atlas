using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalConnectorEndpointTests
{
    [Fact]
    public void Connector_routes_are_owner_only_and_worker_is_registered()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "apps", "api", "Program.cs"));
        var source = File.ReadAllText(Path.Combine(root, "apps", "api", "OperationalConnectorService.cs"));

        Assert.Contains("app.MapOperationalConnectorEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains("AddHostedService<OperationalSyncWorker>", program, StringComparison.Ordinal);
        Assert.Contains("/api/v1/businesses/{businessId:guid}/operational-connector", source, StringComparison.Ordinal);
        Assert.Contains("/sync", source, StringComparison.Ordinal);
        Assert.Contains("/schedule", source, StringComparison.Ordinal);
        Assert.True(Count(source, "RequireAuthorization(\"BusinessOwner\")") >= 5);
        Assert.DoesNotContain("InternalOperator", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Manual_and_scheduled_sync_share_one_service_path()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "apps", "api", "OperationalConnectorService.cs"));
        var worker = File.ReadAllText(Path.Combine(root, "apps", "api", "OperationalSyncWorker.cs"));

        Assert.Contains("SyncBusinessAsync", source, StringComparison.Ordinal);
        Assert.Contains("SyncBusinessAsync", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("webhook", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webhook", worker, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "apps", "api", "Program.cs"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException();
    }
}
