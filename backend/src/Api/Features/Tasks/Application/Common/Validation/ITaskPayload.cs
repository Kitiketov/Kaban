namespace Kaban.Api.Features.Tasks.Application.Common.Validation;

public interface ITaskPayload
{
    string Name { get; }
    string? Description { get; }
    int Priority { get; }
    DateTime? Deadline { get; }
    int? UserId { get; }
    int ColumnId { get; }
    int[]? TagIds { get; }
}
