namespace Kaban.Api.Features.Tasks.Application.Update;

public sealed record TaskMoveRequestDto(
    int ColumnId,
    int Order
);