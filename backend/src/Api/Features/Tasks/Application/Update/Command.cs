using Kaban.Api.Features.Tasks.Application.Common.Dto;
using Kaban.Api.Features.Tasks.Application.Common.Validation;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Update;

public sealed record UpdateTaskCommand(
    int TaskId,
    string Name,
    string? Description,
    int Priority,
    DateTime? Deadline,
    int? UserId,
    int ColumnId,
    int[]? TagIds) : IRequest<TaskResponseDto>, ITaskPayload;
