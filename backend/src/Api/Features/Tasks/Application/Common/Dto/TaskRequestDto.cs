namespace Kaban.Api.Features.Tasks.Application.Common.Dto;

public sealed record TaskRequestDto(
    string Name,
    string? Description,
    int Priority,
    DateTime? Deadline,
    int? UserId,
    int ColumnId,
    int[]? TagIds);