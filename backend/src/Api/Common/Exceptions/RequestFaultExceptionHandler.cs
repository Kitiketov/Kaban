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

<<<<<<< HEAD
        var result = Results.InternalServerError(new
=======
        logger.LogError("Worker request fault for {Method} {Path}", httpContext.Request.Method,
            httpContext.Request.Path);

        var result = Results.BadRequest(new
>>>>>>> origin/init-api
        {
            error = "Request Fault",
            message = requestFaultException.Message
        });

        await result.ExecuteAsync(httpContext);

        return true;
    }
}
