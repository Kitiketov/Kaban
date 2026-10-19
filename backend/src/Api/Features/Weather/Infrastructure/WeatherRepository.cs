using Kaban.Api.Features.Weather.Application.Abstractions;
using Kaban.Api.Features.Weather.Domain;
using Kaban.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Api.Features.Weather.Infrastructure;

// EF Core остаётся в Infrastructure и не попадает в Handler.
public sealed class WeatherRepository(KabanDbContext db) : IWeatherRepository
{
    public async Task<IReadOnlyList<WeatherForecast>> GetAllAsync(CancellationToken ct) =>
        await db.WeatherForecasts.AsNoTracking()
            .OrderByDescending(x => x.Date)
            .Take(100)
            .ToListAsync(ct);

    public Task<WeatherForecast?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.WeatherForecasts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(WeatherForecast forecast, CancellationToken ct)
    {
        await db.WeatherForecasts.AddAsync(forecast, ct);
    }
}
