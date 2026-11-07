using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Create;
using Kaban.Worker.Infrastructure.Database;
using Kaban.Worker.Infrastructure.Database.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Tasks;

public class TaskCreateConsumer(
    KabanDatabaseContext dbContext,
    ILogger<TaskCreateConsumer> logger) : IConsumer<CreateTaskRequest>
{
    public async Task Consume(ConsumeContext<CreateTaskRequest> context)
    {
        var ct = context.CancellationToken;
        var message = context.Message;

        logger.LogInformation("Creating task '{Name}' in column {ColumnId}", message.Name, message.ColumnId);

        await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

        var columnExist = await CheckColumnExist(message.KanbanId, message.ColumnId, ct);

        if (!columnExist)
        {
            await context.RespondAsync(new TaskColumnNotFound
            (
                BoardId: message.KanbanId,
                ColumnId: message.ColumnId
            ));
            return;
        }
        
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({message.ColumnId})", ct);

        var entityTask = await CreateTask(message, ct);

        var userExist = await TryPinTaskToUser(message, entityTask, ct);
        if (!userExist)
        {
            await context.RespondAsync(new TaskUserNotFound
            (
                UserId: message.UserId!.Value
            ));
            return;
        }

        await dbContext.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        await context.RespondAsync(new TaskSuccess
        {
            Id = entityTask.Id,
            Name = entityTask.Name,
            Description = entityTask.Description,
            Priority = (int)entityTask.Priority,
            Deadline = entityTask.Deadline,
            Order = entityTask.TaskOrder,
            ColumnId = entityTask.BoardColumnId
        });
    }

    private async Task<bool> TryPinTaskToUser(CreateTaskRequest message,
        Infrastructure.Database.Domain.Tasks entityTask, CancellationToken ct)
    {
        if (message.UserId is null) return true;
        
        var userExist = await CheckUserExist(message.UserId.Value, message.KanbanId, ct);
        if (!userExist) return false;

        var userTask = new UserTasks
        {
            UserId = message.UserId.Value,
            Task = entityTask
        };
        await dbContext.UserTasks.AddAsync(userTask, ct);

        return true;
    }

    private async Task<Infrastructure.Database.Domain.Tasks> CreateTask(CreateTaskRequest message, CancellationToken ct)
    {
        var maxOrder = await dbContext.Tasks
            .Where(t => t.BoardColumnId == message.ColumnId)
            .Select(t => (int?)t.TaskOrder)
            .MaxAsync(ct) ?? 0;

        var entityTask = new Infrastructure.Database.Domain.Tasks
        {
            Name = message.Name,
            Created = DateTime.UtcNow,
            Description = message.Description,
            Deadline = message.Deadline,
            Priority = (PriorityEnum)message.Priority,
            BoardColumnId = message.ColumnId,
            TaskOrder = maxOrder + 1
        };
        await dbContext.Tasks.AddAsync(entityTask, ct);
        return entityTask;
    }

    private async Task<bool> CheckColumnExist(int boardId, int columnId, CancellationToken ct)
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