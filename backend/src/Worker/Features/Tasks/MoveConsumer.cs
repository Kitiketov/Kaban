using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Move;
using Kaban.Worker.Features.Boards;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Tasks;

public sealed class TaskMoveConsumer(
    KabanDatabaseContext dbContext,
    BoardAccess boardAccess,
    TaskRankService rankService,
    ILogger<TaskMoveConsumer> logger) : IConsumer<MoveTaskRequest>
{
    public async Task Consume(ConsumeContext<MoveTaskRequest> context)
    {
        var ct = context.CancellationToken;
        var request = context.Message;

        logger.LogInformation("Moving task '{TaskId}' to column {ColumnId}", request.TaskId, request.ColumnId);

        var task = await dbContext.Tasks.FindAsync([request.TaskId], ct);
        if (task is null)
        {
            await context.RespondAsync(new TaskNotFound(request.TaskId));
            return;
        }

        if (task.BoardColumnId != request.ColumnId)
        {
            var boardId = await boardAccess.GetCommonBoardIdAsync(task.BoardColumnId, request.ColumnId, ct);
            if (boardId is null)
            {
                await context.RespondAsync(new TaskColumnNotFound(0, request.ColumnId));
                return;
            }
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

        await dbContext.Database.AcquireResourceLockAsync(
            LockResource.BoardColumn, request.ColumnId, ct);

        var rankResult = await rankService.CalculateMoveRankAsync(
            request.ColumnId, request.PrevTaskId, request.NextTaskId, ct);

        if (rankResult.TryPickT1(out var rankError, out var newRank))
        {
            await context.RespondAsync(rankError);
            return;
        }

        if (task.BoardColumnId != request.ColumnId)
            task.MoveToColumn(request.ColumnId, newRank);
        else
            task.SetRank(newRank);

        var assignedUserId = await dbContext.UserTasks
            .Where(ut => ut.TaskId == task.Id)
            .Select(ut => (int?)ut.UserId)
            .FirstOrDefaultAsync(ct);

        await dbContext.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("Moved task {TaskId} to rank {Rank} in column {ColumnId}",
            task.Id, task.TaskRank, task.BoardColumnId);

        await context.RespondAsync(task.ToTaskSuccess(assignedUserId));
    }
}
