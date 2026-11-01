using MassTransit;
using Microsoft.AspNetCore.Diagnostics;

namespace Kaban.Api.Common.Exceptions;

public class RequestFaultExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RequestFaultException requestFaultException)
            return false;

        var result = Results.BadRequest(new
        {
            error = "Request Fault",
            message = requestFaultException.Message
        });

        await result.ExecuteAsync(httpContext);

        return true;
    }
}