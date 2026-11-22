using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Create;
using Kaban.Worker.Features.Boards;
using Kaban.Worker.Features.Tasks.Domain;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using TaskEntity = Kaban.Worker.Features.Tasks.Domain.Tasks;

namespace Kaban.Worker.Features.Tasks;

public sealed class TaskCreateConsumer(
    KabanDatabaseContext dbContext,
    BoardAccess boardAccess,
    TaskRankService rankService,
    ILogger<TaskCreateConsumer> logger) : IConsumer<CreateTaskRequest>
{
    public async Task Consume(ConsumeContext<CreateTaskRequest> context)
    {
        var ct = context.CancellationToken;
        var request = context.Message;

        logger.LogInformation("Creating task '{Name}' in column {ColumnId} of board {BoardId}",
            request.Name, request.ColumnId, request.KanbanId);

        var columnExists = await boardAccess.ColumnExistsOnBoardAsync(request.KanbanId, request.ColumnId, ct);
        if (!columnExists)
        {
            await context.RespondAsync(new TaskColumnNotFound(request.KanbanId, request.ColumnId));
            return;
        }

        if (request.UserId.HasValue)
        {
            var userExists = await boardAccess.HasUserAccessAsync(request.UserId.Value, request.KanbanId, ct);
            if (!userExists)
            {
                await context.RespondAsync(new TaskUserNotFound(request.UserId.Value));
                return;
            }
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

        await dbContext.Database.AcquireResourceLockAsync(
            LockResource.BoardColumn, request.ColumnId, ct);

        var rank = await rankService.GetNextColumnRankAsync(request.ColumnId, ct);

        var task = TaskEntity.Create(
            name: request.Name,
            columnId: request.ColumnId,
            rank: rank,
            description: request.Description,
            priority: (PriorityEnum)request.Priority,
            deadline: request.Deadline
        );

        await dbContext.Tasks.AddAsync(task, ct);

        if (request.UserId.HasValue)
        {
            var userTask = new UserTasks
            {
                UserId = request.UserId.Value,
                Task = task
            };
            await dbContext.UserTasks.AddAsync(userTask, ct);
        }

        await dbContext.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("Created task {TaskId} in column {ColumnId}", task.Id, task.BoardColumnId);

        await context.RespondAsync(task.ToTaskSuccess(request.UserId));
    }
}
