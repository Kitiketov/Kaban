using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Database;

public class KabanContextFactory : IDesignTimeDbContextFactory<KabanDatabaseContext>
{
    public KabanDatabaseContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException(
                "set database connection string to \"ConnectionStrings__DefaultConnection\" env variable");
        var optionsBuilder = new DbContextOptionsBuilder<KabanDatabaseContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new KabanDatabaseContext(optionsBuilder.Options);
    }
}