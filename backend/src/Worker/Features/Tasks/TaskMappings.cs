using Contracts.Features.Tasks.Common;
using TaskEntity = Kaban.Worker.Features.Tasks.Domain.Tasks;

namespace Kaban.Worker.Features.Tasks;

public static class TaskMappings
{
    public static TaskSuccess ToTaskSuccess(this TaskEntity task, int? userId = null)
    {
        return new TaskSuccess
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            Priority = (int)task.Priority,
            Deadline = task.Deadline,
            Rank = task.TaskRank,
            ColumnId = task.BoardColumnId,
            UserId = userId
        };
    }
}
