using MassTransit;
using Microsoft.AspNetCore.Diagnostics;

namespace Kaban.Api.Common.Exceptions;

public class RequestTimeoutExceptionHandler: IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if(exception is not RequestTimeoutException)
            return false;
        
        var result = Results.Json(
            data: new { message = "Timeout occurred" }, 
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
        await result.ExecuteAsync(httpContext);

        return true;
    }
}