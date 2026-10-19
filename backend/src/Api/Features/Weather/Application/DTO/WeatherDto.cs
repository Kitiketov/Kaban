using Kaban.Api.Features.Weather.Domain;

namespace Kaban.Api.Features.Weather.Application.DTO;

// Ответ use-case. Не EF-сущность и не DTO из глобального Client.Models.
public sealed record WeatherDto(
    Guid Id,
    DateOnly Date,
    int TemperatureC,
    string Summary,
    DateTime CreatedAtUtc)
{
    public static WeatherDto FromEntity(WeatherForecast forecast) => new(
        forecast.Id, forecast.Date, forecast.TemperatureC,
        forecast.Summary, forecast.CreatedAtUtc);
}
