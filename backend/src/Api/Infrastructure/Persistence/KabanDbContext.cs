using Kaban.Api.Features.Weather.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Api.Infrastructure.Persistence;

// Один DbContext для БД API; сущности могут принадлежать разным Features.
// Отдельные IEntityTypeConfiguration<> размещаются рядом с соответствующей фичей.
public sealed class KabanDbContext(DbContextOptions<KabanDbContext> options) : DbContext(options)
{
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KabanDbContext).Assembly);
    }
}
