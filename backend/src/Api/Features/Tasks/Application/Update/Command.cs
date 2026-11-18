using Kaban.Api.Features.Tasks.Application.Common.Validation;
using Kaban.Api.Features.Tasks.Application.DTO;
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
    int[]? TagIds) : IRequest<TaskDto>, ITaskPayload;