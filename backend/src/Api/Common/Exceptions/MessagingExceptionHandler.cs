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
            UserOperationException { Code: "idempotency_conflict" } => (409, "Idempotency key was used with a different request"),
            UserOperationException => (400, "Invalid request"),
            RequestTimeoutException => (503, "Worker response timed out"),
            RequestFaultException => (502, "Worker could not complete the request"),
            _ => (0, "")
        };
        if (error.Item1 == 0) return false;
        await Results.Problem(statusCode: error.Item1, title: error.Item2).ExecuteAsync(context);
        return true;
    }
}
