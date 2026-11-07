namespace Kaban.Api.Features.Tasks.Application.Common.Dto;

public sealed record TaskResponseDto(
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