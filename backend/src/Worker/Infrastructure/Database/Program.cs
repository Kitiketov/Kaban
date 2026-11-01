using Kaban.Worker.Infrastructure.Database.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Infrastructure.Database;

public static class TableExtensions
{
    public static string GetDatabaseName(string databaseName)
    {
        Span<char> buffer = stackalloc char[databaseName.Length * 2];
        var pos = 0;
        for (var i = 0; i < databaseName.Length; i++)
        {
            var current = databaseName[i];
            if (char.IsUpper(current))
            {
                if (i > 0)
                    buffer[pos++] = '_';
                buffer[pos++] = char.ToLowerInvariant(current);
            }
            else
                buffer[pos++] = current;
        }
        
        return new string(buffer[..pos]);
    }
}

public class KabanDatabaseContext(DbContextOptions<KabanDatabaseContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresEnum<PriorityEnum>("task_priority");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KabanDatabaseContext).Assembly);
    }
}