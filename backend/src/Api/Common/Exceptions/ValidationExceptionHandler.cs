using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Kaban.Api.Common.Exceptions;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        var errors = validationException.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).ToArray());

        await Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest)
            .ExecuteAsync(httpContext);
        return true;
    }
}
