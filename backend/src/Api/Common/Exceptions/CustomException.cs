namespace Kaban.Api.Common.Exceptions;

public static class CustomException
{
    public static IServiceCollection AddCustomExceptionHandlers(this IServiceCollection services)
    {
        // Явно задаем нужный нам порядок выполнения
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<RequestFaultExceptionHandler>();
        services.AddExceptionHandler<RequestTimeoutExceptionHandler>();

        return services;
    }

}