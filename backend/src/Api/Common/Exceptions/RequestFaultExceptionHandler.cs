using MassTransit;
using Microsoft.AspNetCore.Diagnostics;

namespace Kaban.Api.Common.Exceptions;

public class RequestFaultExceptionHandler(ILogger<RequestFaultExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RequestFaultException requestFaultException)
            return false;

        logger.LogError("Worker request fault for {Method} {Path}", httpContext.Request.Method,
            httpContext.Request.Path);

        var result = Results.InternalServerError(new
        {
            error = "Request Fault",
            message = requestFaultException.Message
        });

        await result.ExecuteAsync(httpContext);

        return true;
    }
}