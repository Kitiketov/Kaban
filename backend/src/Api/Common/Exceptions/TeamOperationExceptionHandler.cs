using Microsoft.AspNetCore.Diagnostics;

namespace Kaban.Api.Common.Exceptions;

public sealed class TeamOperationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not TeamOperationException operationException)
            return false;

        var (statusCode, title) = operationException.Code switch
        {
            "team_not_found" or "user_not_found" or "member_not_found" or "role_not_found" =>
                (StatusCodes.Status404NotFound, "Team resource not found"),
            "not_a_member" => (StatusCodes.Status403Forbidden, "Not a team member"),
            "role_conflict" or "last_member" or "member_conflict" =>
                (StatusCodes.Status409Conflict, "Team operation conflict"),
            _ => (StatusCodes.Status400BadRequest, "Invalid request")
        };

        await Results.Problem(statusCode: statusCode, title: title).ExecuteAsync(httpContext);
        return true;
    }
}
