using Kaban.Api.Features.Tasks.Application.Common.Dto;
using Kaban.Api.Features.Tasks.Application.Common.Validation;
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
    : IRequest<TaskResponseDto>, ITaskPayload;
