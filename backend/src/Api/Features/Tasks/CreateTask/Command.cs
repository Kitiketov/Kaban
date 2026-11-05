using MediatR;

namespace Kaban.Api.Features.Tasks.CreateTask;

public sealed record CreateTaskCommand(
    string Name,
    string? Description,
    int Priority,
    DateTime? Deadline,
    int KanbanId,
    int? UserId,
    int ColumnId,
    int[]? TagIds)
    : IRequest<TaskCreateResponseDto>;