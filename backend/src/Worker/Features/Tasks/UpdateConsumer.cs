using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Update;
using Kaban.Worker.Features.Boards;
using Kaban.Worker.Features.Tasks.Domain;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Tasks;

public sealed class TasksUpdateConsumer(
    KabanDatabaseContext dbContext,
    BoardAccess boardAccess,
    TaskRankService rankService,
    ILogger<TasksUpdateConsumer> logger) : IConsumer<UpdateTaskRequest>
{
    public async Task Consume(ConsumeContext<UpdateTaskRequest> context)
    {
        var ct = context.CancellationToken;
        var request = context.Message;

        logger.LogInformation("Updating task '{TaskId}'", request.TaskId);

        var task = await dbContext.Tasks.FindAsync([request.TaskId], ct);
        if (task is null)
        {
            await context.RespondAsync(new TaskNotFound(request.TaskId));
            return;
        }

        var isColumnChanged = task.BoardColumnId != request.ColumnId;
        int? boardId = null;

        if (isColumnChanged)
        {
            boardId = await boardAccess.GetCommonBoardIdAsync(task.BoardColumnId, request.ColumnId, ct);
            if (boardId is null)
            {
                await context.RespondAsync(new TaskColumnNotFound(0, request.ColumnId));
                return;
            }
        }
        else if (request.UserId.HasValue)
        {
            boardId = await boardAccess.GetBoardIdByColumnAsync(request.ColumnId, ct);
            if (boardId is null)
            {
                await context.RespondAsync(new TaskColumnNotFound(0, request.ColumnId));
                return;
            }
        }

        if (request.UserId.HasValue)
        {
            var userHasAccess = await boardAccess.HasUserAccessAsync(request.UserId.Value, boardId!.Value, ct);
            if (!userHasAccess)
            {
                await context.RespondAsync(new TaskUserNotFound(request.UserId.Value));
                return;
            }
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

        if (isColumnChanged)
        {
            await dbContext.Database.AcquireResourceLockAsync(
                LockResource.BoardColumn, request.ColumnId, ct);

            var newRank = await rankService.GetNextColumnRankAsync(request.ColumnId, ct);
            task.MoveToColumn(request.ColumnId, newRank);
        }

        var existingUserTasks = await dbContext.UserTasks
            .Where(ut => ut.TaskId == task.Id)
            .ToListAsync(ct);

        if (request.UserId.HasValue)
        {
            var targetUserId = request.UserId.Value;
            var isAlreadyAssigned = existingUserTasks.Count == 1 && existingUserTasks[0].UserId == targetUserId;

            if (!isAlreadyAssigned)
            {
                dbContext.UserTasks.RemoveRange(existingUserTasks);
                await dbContext.UserTasks.AddAsync(new UserTasks
                {
                    UserId = targetUserId,
                    TaskId = task.Id
                }, ct);
            }
        }
        else if (existingUserTasks.Count > 0)
        {
            dbContext.UserTasks.RemoveRange(existingUserTasks);
        }

        task.UpdateDetails(
            name: request.Name,
            description: request.Description,
            priority: (PriorityEnum)request.Priority,
            deadline: request.Deadline
        );

        await dbContext.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("Updated task '{TaskId}'", task.Id);

        await context.RespondAsync(task.ToTaskSuccess(request.UserId));
    }
}
