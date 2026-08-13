using Atlas.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalSignalPersistenceTests
{
    [Fact]
    public void Durable_operational_entities_expose_no_raw_csv_or_customer_payload()
    {
        var forbidden = new[] { "Raw", "Csv", "CustomerName", "CustomerEmail", "CustomerPhone", "Address", "Notes", "Pan", "Cvv" };
        var durableTypes = new[]
        {
            typeof(OperationalConnector), typeof(OperationalFileCheckpoint), typeof(OperationalImport),
            typeof(BusinessSignal), typeof(BusinessChange)
        };

        foreach (var type in durableTypes)
        foreach (var property in type.GetProperties())
            Assert.DoesNotContain(forbidden, token => property.Name.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DbContext_exposes_business_scoped_operational_sets_and_unique_identities()
    {
        using var db = new AtlasDbContext(new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        Assert.NotNull(db.OperationalConnectors);
        Assert.NotNull(db.OperationalFileCheckpoints);
        Assert.NotNull(db.OperationalImports);
        Assert.NotNull(db.BusinessSignals);
        Assert.NotNull(db.BusinessChanges);

        var model = db.Model;
        AssertUniqueIndex<OperationalConnector>(model, nameof(OperationalConnector.BusinessId));
        AssertUniqueIndex<OperationalFileCheckpoint>(model, nameof(OperationalFileCheckpoint.BusinessId), nameof(OperationalFileCheckpoint.ProviderFileId));
        AssertUniqueIndex<BusinessSignal>(model, nameof(BusinessSignal.BusinessId), nameof(BusinessSignal.Identity));
        AssertUniqueIndex<BusinessChange>(model, nameof(BusinessChange.BusinessId), nameof(BusinessChange.Identity));
    }

    [Fact]
    public void Signal_value_uses_bounded_financial_precision()
    {
        using var db = new AtlasDbContext(new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var property = db.Model.FindEntityType(typeof(BusinessSignal))!.FindProperty(nameof(BusinessSignal.Value))!;
        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(4, property.GetScale());
    }

    private static void AssertUniqueIndex<T>(Microsoft.EntityFrameworkCore.Metadata.IModel model, params string[] properties)
    {
        var match = model.FindEntityType(typeof(T))!.GetIndexes().SingleOrDefault(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(properties));
        Assert.NotNull(match);
        Assert.True(match!.IsUnique);
    }
}
