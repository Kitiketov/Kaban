namespace Kaban.Api.Features.Tasks.DTO;

public sealed record TaskCreateRequestDto(
    string Name,
    string? Description,
    int Priority,
    DateTime? Deadline,
    int? UserId,
    int ColumnId,
    int[]? TagIds);

public sealed record TaskCreateResponseDto(
    int Id,
    string Name,
    string? Description,
    int Priority,
    DateTime? Deadline,
    int KanbanId,
    int? UserId,
    int Order,
    int ColumnId,
    int[]? TagIds
);
