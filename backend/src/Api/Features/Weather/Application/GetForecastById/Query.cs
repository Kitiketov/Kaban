using Kaban.Api.Features.Weather.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Weather.Application.GetForecastById;

public sealed record GetForecastByIdQuery(Guid Id) : IRequest<WeatherDto?>;
