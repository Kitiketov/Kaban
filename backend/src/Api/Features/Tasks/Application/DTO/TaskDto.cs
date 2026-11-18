using Contracts.Features.Tasks.Common;

namespace Kaban.Api.Features.Tasks.Application.DTO;

public sealed record TaskDto(
    int Id,
    string Name,
    string? Description,
    int Priority,
    DateTime? Deadline,
    int? KanbanId,
    int? UserId,
    string Rank,
    int ColumnId,
    int[]? TagIds
)
{
    public static TaskDto FromSuccess(
        TaskSuccess taskSuccess,
        int? kanbanId = null,
        int? userId = null,
        int[]? tagIds = null)
    {
        return new TaskDto(
            taskSuccess.Id,
            taskSuccess.Name,
            taskSuccess.Description,
            taskSuccess.Priority,
            taskSuccess.Deadline,
            kanbanId,
            userId,
            taskSuccess.Rank,
            taskSuccess.ColumnId,
            tagIds
        );
    }
}
