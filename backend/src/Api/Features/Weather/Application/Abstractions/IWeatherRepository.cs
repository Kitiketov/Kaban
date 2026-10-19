using Kaban.Api.Features.Weather.Domain;

namespace Kaban.Api.Features.Weather.Application.Abstractions;

public interface IWeatherRepository
{
    Task<IReadOnlyList<WeatherForecast>> GetAllAsync(CancellationToken ct);
    Task<WeatherForecast?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(WeatherForecast forecast, CancellationToken ct);
}
