using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaban.Worker.Infrastructure.Database;

public class KabanContextFactory : IDesignTimeDbContextFactory<KabanDatabaseContext>
{
    public KabanDatabaseContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        var optionsBuilder = new DbContextOptionsBuilder<KabanDatabaseContext>();
        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IHistoryRepository, KabanHistoryRepository>();
        return new KabanDatabaseContext(optionsBuilder.Options);
    }
}