namespace Kaban.Api.Features.Tasks.Application.Move;

public sealed record TaskMoveRequestDto(
    int TaskId,
    int ColumnId,
    int Order
);