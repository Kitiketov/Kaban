using Microsoft.AspNetCore.Diagnostics;

namespace Kaban.Api.Common.Exceptions;

public sealed class NotFoundExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException notFoundException)
            return false;

        var problemDetails = Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Resource Not Found",
            detail: notFoundException.Message
        );

        await problemDetails.ExecuteAsync(httpContext);
        return true;
    }
}