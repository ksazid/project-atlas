using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Atlas.Api;

public sealed class AtlasDbContextFactory : IDesignTimeDbContextFactory<AtlasDbContext>
{
    public AtlasDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Atlas")
            ?? "Host=localhost;Port=5432;Database=atlas;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AtlasDbContext(options);
    }
}
