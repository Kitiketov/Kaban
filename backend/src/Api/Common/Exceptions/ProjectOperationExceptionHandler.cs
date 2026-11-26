using Microsoft.AspNetCore.Diagnostics;

namespace Kaban.Api.Common.Exceptions;

public sealed class ProjectOperationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ProjectOperationException operationException)
            return false;

        var (statusCode, title) = operationException.Code switch
        {
            "project_not_found" => (StatusCodes.Status404NotFound, "Project not found"),
            "team_not_found" => (StatusCodes.Status404NotFound, "Team resource not found"),
            "not_a_member" => (StatusCodes.Status403Forbidden, "Not a team member"),
            _ => (StatusCodes.Status400BadRequest, "Invalid request")
        };

        await Results.Problem(statusCode: statusCode, title: title).ExecuteAsync(httpContext);
        return true;
    }
}