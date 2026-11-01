namespace Kaban.Api.Features.Tasks.DTO;

public sealed record TaskCreateRequestDto(
    string Name,
    string Description,
    int Priority,
    DateTime Deadline,
    int BoardId,
    int UserId,
    int ColumnId,
    int Order,
    int[] TagIds);

public sealed record TaskCreateResponseDto(
    int Id,
    string Name,
    string Description,
    int Priority,
    DateTime Deadline,
    int BoardId,
    int UserId,
    int ColumnId,
    int Order,
    int[] TagIds
);
