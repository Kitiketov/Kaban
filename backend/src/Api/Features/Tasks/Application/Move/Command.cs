using Kaban.Api.Features.Tasks.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Move;

public sealed record MoveTaskCommand(
    int TaskId,
    int ColumnId,
    int? PrevTaskId,
    int? NextTaskId) : IRequest<TaskDto>;