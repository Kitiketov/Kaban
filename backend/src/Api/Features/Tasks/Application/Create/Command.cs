using Kaban.Api.Features.Tasks.Application.Common.Validation;
using Kaban.Api.Features.Tasks.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Create;

public sealed record CreateTaskCommand(
    string Name,
    string? Description,
    int Priority,
    DateTime? Deadline,
    int KanbanId,
    int? UserId,
    int ColumnId,
    int[]? TagIds)
    : IRequest<TaskDto>, ITaskPayload;