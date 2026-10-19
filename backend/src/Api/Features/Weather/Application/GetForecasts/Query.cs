using Kaban.Api.Features.Weather.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Weather.Application.GetForecasts;

public sealed record GetForecastsQuery() : IRequest<IReadOnlyList<WeatherDto>>;
