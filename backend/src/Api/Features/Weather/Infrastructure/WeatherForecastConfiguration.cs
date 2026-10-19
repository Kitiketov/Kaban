using Kaban.Api.Features.Weather.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaban.Api.Features.Weather.Infrastructure;

// Конфигурация таблицы находится в Weather, не в гигантском DbContext.
public sealed class WeatherForecastConfiguration : IEntityTypeConfiguration<WeatherForecast>
{
    public void Configure(EntityTypeBuilder<WeatherForecast> builder)
    {
        builder.ToTable("WeatherForecasts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Summary).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => x.Date);
    }
}
