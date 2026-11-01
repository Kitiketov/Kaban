namespace Contracts.Weather;

public sealed record ForecastCreatedEvent(DateOnly Date, int TemperatureC, string Summary);