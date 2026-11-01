using Contracts.Weather;
using MassTransit;

namespace Kaban.Worker.Features.Weather;

public class ForecastCreatedConsumer(ILogger<ForecastCreatedConsumer> logger) : IConsumer<ForecastCreatedEvent>
{
    public Task Consume(ConsumeContext<ForecastCreatedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handel event ${typeof(ForecastCreatedEvent)} " +
        $": {message.Date} {message.Summary} {message.TemperatureC}");
        return Task.CompletedTask;
    }
}