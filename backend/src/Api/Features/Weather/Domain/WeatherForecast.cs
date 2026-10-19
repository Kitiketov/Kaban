namespace Kaban.Api.Features.Weather.Domain;

// Domain object does not depend on HTTP, RabbitMQ, or EF Core.
public sealed class WeatherForecast
{
    private WeatherForecast() { } // EF Core

    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public int TemperatureC { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static WeatherForecast Create(DateOnly date, int temperatureC, string summary)
    {
        if (temperatureC is < -100 or > 100)
            throw new ArgumentOutOfRangeException(nameof(temperatureC));
        if (string.IsNullOrWhiteSpace(summary) || summary.Trim().Length > 120)
            throw new ArgumentException("Summary must be between 1 and 120 characters", nameof(summary));

        return new WeatherForecast
        {
            Id = Guid.NewGuid(),
            Date = date,
            TemperatureC = temperatureC,
            Summary = summary.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
