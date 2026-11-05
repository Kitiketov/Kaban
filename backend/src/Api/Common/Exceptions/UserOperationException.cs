namespace Kaban.Api.Common.Exceptions;

public sealed class UserOperationException(string code) : Exception("User operation rejected.")
{
    public string Code { get; } = code;
}
