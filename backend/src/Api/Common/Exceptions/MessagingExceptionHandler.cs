using MassTransit;
using Microsoft.AspNetCore.Diagnostics;

namespace Kaban.Api.Common.Exceptions;

public sealed class MessagingExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var error = exception switch
        {
            UserOperationException { Code: "not_found" } => (404, "User not found"),
            UserOperationException { Code: "invalid_credentials" or "unauthorized" } => (401, "Invalid credentials"),
            UserOperationException { Code: "email_conflict" } => (409, "Email is already registered"),
            UserOperationException => (400, "Invalid request"),
            TeamOperationException { Code: "team_not_found" or "user_not_found" or "member_not_found" or "role_not_found" } =>
                (404, "Team resource not found"),
            TeamOperationException { Code: "not_a_member" } => (403, "Not a team member"),
            TeamOperationException { Code: "role_conflict" or "last_member" or "member_conflict" } =>
                (409, "Team operation conflict"),
            TeamOperationException => (400, "Invalid request"),
            ProjectOperationException { Code: "project_not_found" or "team_not_found" } => (404, "Project resource not found"),
            ProjectOperationException { Code: "not_a_member" } => (403, "Not a team member"),
            ProjectOperationException => (400, "Invalid request"),
            RequestTimeoutException => (503, "Worker response timed out"),
            RequestFaultException => (502, "Worker could not complete the request"),
            _ => (0, "")
        };
        if (error.Item1 == 0) return false;
        await Results.Problem(statusCode: error.Item1, title: error.Item2).ExecuteAsync(context);
        return true;
    }
}
