using Kaban.Api.Features.Tasks.DTO;
using MediatR;

namespace Kaban.Api.Features.Tasks.CreateTask;

public sealed record CreateTaskCommand(
    string Name,
    string Description,
    int Priority,
    DateTime Deadline,
    int BoardId,
    int UserId,
    int ColumnId,
    int Order,
    int[] TagIds)
    : IRequest<TaskCreateResponseDto>;
