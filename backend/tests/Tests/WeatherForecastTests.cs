using Kaban.Api.Features.Weather.Domain;
using Xunit;

namespace Kaban.Tests;

public sealed class WeatherForecastTests
{
    [Fact]
    public void Create_ValidData_GeneratesId()
    {
        var forecast = WeatherForecast.Create(new DateOnly(2026, 10, 19), 12, "Cloudy");
        Assert.NotEqual(Guid.Empty, forecast.Id);
        Assert.Equal("Cloudy", forecast.Summary);
    }

    [Fact]
    public void Create_BlankSummary_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            WeatherForecast.Create(new DateOnly(2026, 10, 19), 12, "  "));
    }
}
