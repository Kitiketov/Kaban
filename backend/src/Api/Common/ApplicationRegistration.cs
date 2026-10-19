using FluentValidation;
using Kaban.Api.Common.Validation;
using Kaban.Api.Features.Weather.Application.CreateForecast;
using MediatR;

namespace Kaban.Api.Common;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        services.AddValidatorsFromAssemblyContaining<CreateForecastValidator>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
