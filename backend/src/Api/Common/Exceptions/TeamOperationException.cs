namespace Kaban.Api.Common.Exceptions;

public sealed class TeamOperationException(string code) : Exception("Team operation rejected.")
{
    public string Code { get; } = code;
}
