namespace Contracts.Features.Tasks.Update;

public sealed record UpdateTaskRequest(
    int TaskId,
    string Name,
    string? Description,
    int Priority,
    DateTime? Deadline,
    int? UserId,
    int ColumnId,
    int[]? TagIds);