using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Create;
using Contracts.Features.Tasks.Delete;
using Contracts.Features.Tasks.Move;
using Contracts.Features.Tasks.Update;
using Kaban.Worker.Infrastructure.Database;
using Kaban.Worker.Infrastructure.Database.Domain;
using LexoAlgorithm;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Kaban.Worker.Features.Tasks.Application;

public class TaskService(KabanDatabaseContext dbContext) : ITaskService
{
    public async Task<OneOf<TaskSuccess,
        TaskColumnNotFound,
        TaskUserNotFound>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

        var columnExist = await CheckColumnExistOnBoard(request.KanbanId, request.ColumnId, ct);

        if (!columnExist)
        {
            return new TaskColumnNotFound
            (
                BoardId: request.KanbanId,
                ColumnId: request.ColumnId
            );
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({request.ColumnId})", ct);

        var entityTask = await CreateTask(request, ct);

        var userExist = await TryPinTaskToUser(request.UserId, request.KanbanId, entityTask, ct);
        if (!userExist)
        {
            return new TaskUserNotFound
            (
                UserId: request.UserId!.Value
            );
        }

        await dbContext.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return new TaskSuccess
        {
            Id = entityTask.Id,
            Name = entityTask.Name,
            Description = entityTask.Description,
            Priority = (int)entityTask.Priority,
            Deadline = entityTask.Deadline,
            Rank = entityTask.TaskRank,
            ColumnId = entityTask.BoardColumnId,
            UserId = request.UserId
        };
    }

    public async Task<OneOf<DeleteTaskSuccess, TaskNotFound>> Delete(DeleteTaskRequest request, CancellationToken ct)
    {
        var taskId = request.TaskId;
        var task = await dbContext.Tasks.FindAsync(taskId, ct);
        if (task is null)
        {
            return new TaskNotFound(taskId);
        }

        dbContext.Tasks.Remove(task);
        dbContext.UserTasks.RemoveRange(dbContext.UserTasks.Where(ut => ut.TaskId == taskId));

        await dbContext.SaveChangesAsync(ct);

        return new DeleteTaskSuccess(taskId);
    }

    public async Task<OneOf<TaskSuccess, TaskNotFound, TaskColumnNotFound, TaskNotInColumn>> Move(
        MoveTaskRequest request, CancellationToken ct)
    {
        var task = await dbContext.Tasks.FindAsync(request.TaskId, ct);
        if (task is null)
            return new TaskNotFound(request.TaskId);

        if (task.BoardColumnId != request.ColumnId)
        {
            var kanbanIdIfFailed = await TryColumnChange(task, request.ColumnId, ct);
            if (kanbanIdIfFailed is not null)
                return new TaskColumnNotFound(kanbanIdIfFailed.Value, request.ColumnId);
        }

        Infrastructure.Database.Domain.Tasks? prevTask = null;
        Infrastructure.Database.Domain.Tasks? nextTask = null;

        if (request.PrevTaskId is not null)
        {
            prevTask = await dbContext.Tasks.FindAsync(request.PrevTaskId, ct);
            if (prevTask is null)
                return new TaskNotFound(request.PrevTaskId.Value);
            if (prevTask.BoardColumnId != task.BoardColumnId)
            {
                return new TaskNotInColumn(prevTask.Id, task.BoardColumnId);
            }
        }

        if (request.NextTaskId is not null)
        {
            nextTask = await dbContext.Tasks.FindAsync(request.NextTaskId, ct);
            if (nextTask is null)
                return new TaskNotFound(request.NextTaskId.Value);
            if (nextTask.BoardColumnId != task.BoardColumnId)
            {
                return new TaskNotInColumn(nextTask.Id, task.BoardColumnId);
            }
        }

        UpdateRank(task, prevTask?.TaskRank, nextTask?.TaskRank);

        dbContext.Update(task);
        await dbContext.SaveChangesAsync(ct);
        return new TaskSuccess
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            Priority = (int)task.Priority,
            Deadline = task.Deadline,
            Rank = task.TaskRank,
            ColumnId = task.BoardColumnId
        };
    }

    public async Task<OneOf<TaskSuccess, TaskNotFound, TaskColumnNotFound, TaskUserNotFound>> Update(
        UpdateTaskRequest request, CancellationToken ct)
    {
        var task = await dbContext.Tasks.FindAsync(request.TaskId, ct);
        if (task is null)
            return new TaskNotFound(request.TaskId);

        if (task.BoardColumnId != request.ColumnId)
        {
            var kanbanIdIfFailed = await TryColumnChange(task, request.ColumnId, ct);
            if (kanbanIdIfFailed is not null)
                return new TaskColumnNotFound(kanbanIdIfFailed.Value, request.ColumnId);
            task.TaskRank = await FindNextColumnRank(task.BoardColumnId, ct);
        }

        var boardId = await GetBoardIdByColumn(task.BoardColumnId, ct);

        var userExist = await TrySyncUserTask(request.UserId, boardId, task, ct);

        if (!userExist)
        {
            return new TaskUserNotFound
            (
                UserId: request.UserId!.Value
            );
        }

        task.Name = request.Name;
        task.Description = request.Description;
        task.Deadline = request.Deadline;
        task.Priority = (PriorityEnum)request.Priority;

        dbContext.Update(task);
        await dbContext.SaveChangesAsync(ct);

        return new TaskSuccess
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            Priority = (int)task.Priority,
            Deadline = task.Deadline,
            Rank = task.TaskRank,
            ColumnId = task.BoardColumnId
        };
    }

    private static void UpdateRank(Infrastructure.Database.Domain.Tasks task, string? prevTaskTaskRank,
        string? nextTaskTaskRank)
    {
        if (prevTaskTaskRank is null && nextTaskTaskRank is null)
        {
            task.TaskRank = LexoRank.Middle().ToString();
            return;
        }

        if (prevTaskTaskRank is not null && nextTaskTaskRank is not null)
        {
            task.TaskRank = LexoRank.Parse(prevTaskTaskRank).Between(LexoRank.Parse(nextTaskTaskRank)).ToString();
            return;
        }

        if (prevTaskTaskRank is not null)
        {
            task.TaskRank = LexoRank.Parse(prevTaskTaskRank).GenNext().ToString();
            return;
        }

        task.TaskRank = LexoRank.Parse(nextTaskTaskRank).GenPrev().ToString();
    }


    private async Task<int?> TryColumnChange(Infrastructure.Database.Domain.Tasks task, int newColumnId,
        CancellationToken ct)
    {
        var boardId = await GetBoardIdByColumn(task.BoardColumnId, ct);
        var columnExist = await CheckColumnExistOnBoard(boardId, newColumnId, ct);
        if (!columnExist)
            return boardId;
        task.BoardColumnId = newColumnId;

        return null;
    }

    private async Task<int> GetBoardIdByColumn(int taskBoardColumnId, CancellationToken ct)
    {
        var boardColumn = await dbContext.BoardColumns.FindAsync(taskBoardColumnId, ct);
        return boardColumn?.BoardId ??
               throw new InvalidOperationException($"Board column {taskBoardColumnId} not found in table");
    }

    private async Task<bool> TryPinTaskToUser(int? userId, int kanbanId,
        Infrastructure.Database.Domain.Tasks entityTask, CancellationToken ct)
    {
        if (userId is null) return true;
        var currentUser = await dbContext.UserTasks.FindAsync(entityTask.Id, userId, ct);
        if (userId == currentUser?.UserId) return true;
        var userExist = await CheckUserExist(userId.Value, kanbanId, ct);
        if (!userExist) return false;

        var userTask = new UserTasks
        {
            UserId = userId.Value,
            Task = entityTask
        };
        await dbContext.UserTasks.AddAsync(userTask, ct);

        return true;
    }

    private async Task<bool> TrySyncUserTask(int? userId, int kanbanId,
        Infrastructure.Database.Domain.Tasks task, CancellationToken ct)
    {
        if (userId.HasValue)
        {
            var userExist = await CheckUserExist(userId.Value, kanbanId, ct);
            if (!userExist) return false;
        }

        var existingUserTasks = await dbContext.UserTasks
            .Where(ut => ut.TaskId == task.Id)
            .ToListAsync(ct);

        if (userId.HasValue)
        {
            if (existingUserTasks.Count == 1 && existingUserTasks[0].UserId == userId.Value)
                return true;

            dbContext.UserTasks.RemoveRange(existingUserTasks);
            await dbContext.UserTasks.AddAsync(new UserTasks
            {
                UserId = userId.Value,
                TaskId = task.Id
            }, ct);
        }
        else
        {
            if (existingUserTasks.Count > 0) dbContext.UserTasks.RemoveRange(existingUserTasks);
        }

        return true;
    }

    private async Task<Infrastructure.Database.Domain.Tasks> CreateTask(CreateTaskRequest message, CancellationToken ct)
    {
        var rank = await FindNextColumnRank(message.ColumnId, ct);
        var entityTask = new Infrastructure.Database.Domain.Tasks
        {
            Name = message.Name,
            Created = DateTime.UtcNow,
            Description = message.Description,
            Deadline = message.Deadline,
            Priority = (PriorityEnum)message.Priority,
            BoardColumnId = message.ColumnId,
            TaskRank = rank
        };
        await dbContext.Tasks.AddAsync(entityTask, ct);
        return entityTask;
    }

    private async Task<string> FindNextColumnRank(int columnId, CancellationToken ct)
    {
        var maxRankStr = await dbContext.Tasks
            .Where(t => t.BoardColumnId == columnId)
            .Select(t => (string?)t.TaskRank)
            .MaxAsync(ct);
        if (maxRankStr == null)
            return LexoRank.Middle().ToString();

        var maxRank = LexoRank.Parse(maxRankStr);
        return maxRank.GenNext().ToString();
    }

    private async Task<bool> CheckColumnExistOnBoard(int boardId, int columnId, CancellationToken ct)
        => await dbContext.BoardColumns
            .AnyAsync(x => x.Id == columnId && x.BoardId == boardId, ct);

    // TODO сдедать проверку на каждое из условий и возвращать понятную ошибку на каждую
    private async Task<bool> CheckUserExist(int userId, int kanbanId, CancellationToken ct)
        => await dbContext.UserTeams
            .AnyAsync(ut => ut.UserId == userId &&
                            dbContext.Boards.Any(b => b.Id == kanbanId &&
                                                      dbContext.TeamProjects.Any(tp =>
                                                          tp.ProjectId == b.ProjectId && tp.TeamId == ut.TeamId)), ct);
}