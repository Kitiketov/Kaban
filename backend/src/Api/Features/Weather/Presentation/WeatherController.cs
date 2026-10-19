using Kaban.Api.Features.Weather.Application.CreateForecast;
using Kaban.Api.Features.Weather.Application.DTO;
using Kaban.Api.Features.Weather.Application.GetForecastById;
using Kaban.Api.Features.Weather.Application.GetForecasts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kaban.Api.Features.Weather.Presentation;

[ApiController]
[Route("api/weather")]
public sealed class WeatherController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WeatherDto>>> GetAll(CancellationToken ct) =>
        Ok(await mediator.Send(new GetForecastsQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WeatherDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetForecastByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<WeatherDto>> Create(
        [FromBody] CreateForecastRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateForecastCommand(request.Date, request.TemperatureC, request.Summary), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
