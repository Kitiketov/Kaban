using Contracts.Features.Tasks.Common;

namespace Kaban.Api.Features.Tasks.Application.Common.Dto;

public sealed record TaskResponseDto(
    int Id,
    string Name,
    string? Description,
    int Priority,
    DateTime? Deadline,
    int? KanbanId,
    int? UserId,
    int Order,
    int ColumnId,
    int[]? TagIds
)
{
    public static TaskResponseDto FromSuccess(
        TaskSuccess taskSuccess,
        int? kabanId = null,
        int? userId = null,
        int[]? tagIds = null)
    {
        return new TaskResponseDto(
            taskSuccess.Id,
            taskSuccess.Name,
            taskSuccess.Description,
            taskSuccess.Priority,
            taskSuccess.Deadline,
            kabanId,
            userId,
            taskSuccess.Order,
            taskSuccess.ColumnId,
            tagIds
        );
    }
}