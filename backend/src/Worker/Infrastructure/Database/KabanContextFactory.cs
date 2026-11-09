using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kaban.Worker.Infrastructure.Database;

public class KabanContextFactory : IDesignTimeDbContextFactory<KabanDatabaseContext>
{
    public KabanDatabaseContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        var optionsBuilder = new DbContextOptionsBuilder<KabanDatabaseContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new KabanDatabaseContext(optionsBuilder.Options);
    }
}