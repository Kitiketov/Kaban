using Microsoft.AspNetCore.Diagnostics;

namespace Kaban.Api.Common.Exceptions;

public sealed class UserOperationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UserOperationException operationException)
            return false;

        var (statusCode, title) = operationException.Code switch
        {
            "not_found" => (StatusCodes.Status404NotFound, "User not found"),
            "invalid_credentials" or "unauthorized" =>
                (StatusCodes.Status401Unauthorized, "Invalid credentials"),
            "email_conflict" => (StatusCodes.Status409Conflict, "Email is already registered"),
            _ => (StatusCodes.Status400BadRequest, "Invalid request")
        };

        await Results.Problem(statusCode: statusCode, title: title).ExecuteAsync(httpContext);
        return true;
    }
}
