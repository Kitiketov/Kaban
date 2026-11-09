namespace Kaban.Api.Features.Tasks.Application.Move;

public sealed record TaskMoveRequestDto(
    int ColumnId,
    int? PrevTaskId,
    int? NextTaskId
);