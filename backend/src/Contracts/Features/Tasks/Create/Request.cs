namespace Contracts.Features.Tasks.Create;

public sealed record CreateTaskRequest(
    string Name,
    string? Description,
    int Priority,
    DateTime? Deadline,
    int KanbanId,
    int? UserId,
    int ColumnId,
    int[]? TagIds);