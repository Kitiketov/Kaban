using FluentValidation;
using Kaban.Api.Common.Validation;
using MediatR;
using FluentValidation;

namespace Kaban.Api.Common;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        return services;
    }
}