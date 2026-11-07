using Kaban.Api.Features.Tasks.Application.Common.Dto;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Move;

public sealed record MoveTaskCommand(
    int TaskId,
    int ColumnId,
    int Order) : IRequest<TaskResponseDto>;