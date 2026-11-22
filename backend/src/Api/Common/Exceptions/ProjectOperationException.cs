namespace Kaban.Api.Common.Exceptions;

public sealed class ProjectOperationException(string code) : Exception("Project operation rejected.")
{
    public string Code { get; } = code;
}
