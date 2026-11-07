namespace Contracts.Features.Tasks.Move;

public sealed record MoveTaskRequest(
    int TaskId,
    int ColumnId,
    int Order);
