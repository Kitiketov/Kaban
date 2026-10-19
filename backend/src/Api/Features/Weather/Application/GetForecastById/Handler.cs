using Kaban.Api.Features.Weather.Application.Abstractions;
using Kaban.Api.Features.Weather.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Weather.Application.GetForecastById;

public sealed class GetForecastByIdHandler(IWeatherRepository repository)
    : IRequestHandler<GetForecastByIdQuery, WeatherDto?>
{
    public async Task<WeatherDto?> Handle(GetForecastByIdQuery query, CancellationToken ct)
    {
        var forecast = await repository.GetByIdAsync(query.Id, ct);
        return forecast is null ? null : WeatherDto.FromEntity(forecast);
    }
}
