using Kaban.Api.Features.Weather.Application.Abstractions;

namespace Kaban.Api.Features.Weather.Infrastructure;

// DI именно этой фичи. Новая фича регистрирует только свои реализации.
public static class WeatherRegistration
{
    public static IServiceCollection AddWeatherFeature(this IServiceCollection services)
    {
        services.AddScoped<IWeatherRepository, WeatherRepository>();
        return services;
    }
}
