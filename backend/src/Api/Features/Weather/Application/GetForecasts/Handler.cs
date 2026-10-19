using Kaban.Api.Features.Weather.Application.Abstractions;
using Kaban.Api.Features.Weather.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Weather.Application.GetForecasts;

public sealed class GetForecastsHandler(IWeatherRepository repository)
    : IRequestHandler<GetForecastsQuery, IReadOnlyList<WeatherDto>>
{
    public async Task<IReadOnlyList<WeatherDto>> Handle(GetForecastsQuery query, CancellationToken ct)
    {
        var forecasts = await repository.GetAllAsync(ct);
        return forecasts.Select(WeatherDto.FromEntity).ToArray();
    }
}
