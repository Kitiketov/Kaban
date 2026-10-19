using Kaban.Api.Features.Weather.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Weather.Application.CreateForecast;

public sealed record CreateForecastCommand(DateOnly Date, int TemperatureC, string Summary)
    : IRequest<WeatherDto>;
