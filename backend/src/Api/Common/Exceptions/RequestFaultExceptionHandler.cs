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

        await Results.Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "Worker could not complete the request").ExecuteAsync(httpContext);

        return true;
    }
}