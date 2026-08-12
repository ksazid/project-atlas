using System.Reflection;
using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class PilotOperationsPersistenceTests
{
    private static Assembly ApiAssembly => typeof(AtlasDbContext).Assembly;

    [Fact]
    public void Pilot_operations_records_are_registered_in_the_model()
    {
        var intelligenceType = RequireType("IntelligenceRunRecord");
        var operationType = RequireType("PilotOperationRecord");

        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=atlas;Username=postgres;Password=postgres")
            .Options;
        using var db = new AtlasDbContext(options);

        var intelligence = db.Model.FindEntityType(intelligenceType);
        var operation = db.Model.FindEntityType(operationType);
        Assert.NotNull(intelligence);
        Assert.NotNull(operation);

        Assert.Equal(40, intelligence!.FindProperty("Outcome")!.GetMaxLength());
        Assert.Equal(120, intelligence.FindProperty("Code")!.GetMaxLength());
        Assert.Equal(40, operation!.FindProperty("Action")!.GetMaxLength());
        Assert.Equal(40, operation.FindProperty("TargetType")!.GetMaxLength());
        Assert.Equal(2000, operation.FindProperty("Reason")!.GetMaxLength());
        Assert.Equal("jsonb", operation.FindProperty("MetadataJson")!.GetColumnType());

        Assert.Contains(intelligence.GetIndexes(), x => PropertyNames(x.Properties).SequenceEqual(["BusinessId", "OccurredAt"]));
        Assert.Contains(operation.GetIndexes(), x => PropertyNames(x.Properties).SequenceEqual(["BusinessId", "OccurredAt"]));
    }

    [Fact]
    public void DbContext_exposes_pilot_operation_sets_and_forward_only_migration()
    {
        Assert.NotNull(typeof(AtlasDbContext).GetProperty("IntelligenceRuns"));
        Assert.NotNull(typeof(AtlasDbContext).GetProperty("PilotOperationRecords"));
        Assert.NotNull(ApiAssembly.GetType("Atlas.Api.Migrations.PilotOperations"));
    }

    private static string[] PropertyNames(IReadOnlyList<Microsoft.EntityFrameworkCore.Metadata.IProperty> properties) =>
        properties.Select(x => x.Name).ToArray();

    private static Type RequireType(string name)
    {
        var type = ApiAssembly.GetType($"Atlas.Api.{name}");
        Assert.NotNull(type);
        return type!;
    }
}
