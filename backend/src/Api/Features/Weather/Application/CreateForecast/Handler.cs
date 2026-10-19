using Kaban.Api.Common.Application;
using Kaban.Api.Features.Weather.Application.Abstractions;
using Kaban.Api.Features.Weather.Application.DTO;
using Kaban.Api.Features.Weather.Domain;
using MediatR;

namespace Kaban.Api.Features.Weather.Application.CreateForecast;

public sealed class CreateForecastHandler(
    IWeatherRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateForecastCommand, WeatherDto>
{
    public async Task<WeatherDto> Handle(CreateForecastCommand command, CancellationToken ct)
    {
        var forecast = WeatherForecast.Create(command.Date, command.TemperatureC, command.Summary);
        await repository.AddAsync(forecast, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return WeatherDto.FromEntity(forecast);
    }
}
