using MassTransit;
using Microsoft.AspNetCore.Diagnostics;

namespace Kaban.Api.Common.Exceptions;

public class RequestTimeoutExceptionHandler(ILogger<RequestTimeoutExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RequestTimeoutException)
            return false;

        logger.LogWarning("Worker request timed out for {Method} {Path}", httpContext.Request.Method,
            httpContext.Request.Path);

        await Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Worker response timed out").ExecuteAsync(httpContext);

        return true;
    }
}
