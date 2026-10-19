namespace Kaban.Api.Features.Weather.Presentation;

// HTTP DTO. Конвертируется в Command прямо в контроллере.
public sealed record CreateForecastRequest(DateOnly Date, int TemperatureC, string Summary);
