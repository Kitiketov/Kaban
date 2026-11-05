using Contracts.Features.Tasks;
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
        logger.LogInformation(
            "Handled event {EventType}: {Name}",
            nameof(CreateTaskRequest),
            message.Name);
        
        var entityTask = new Infrastructure.Database.Domain.Tasks
        {
            Name = message.Name,
            Created = DateTime.UtcNow,
            Description = string.IsNullOrEmpty(message.Description) ? null : message.Description,
            Deadline = message.Deadline,
            Priority = (PriorityEnum)message.Priority,
            BoardColumnId = message.ColumnId,
        };
        
        var maxOrder = await dbContext.Tasks
            .Where(t => t.BoardColumnId == message.ColumnId)
            .Select(t => (int?)t.TaskOrder)
            .MaxAsync(context.CancellationToken) ?? 0;
   
        entityTask.TaskOrder = maxOrder + 1;
        
        await dbContext.AddAsync(entityTask, ct);
        await dbContext.SaveChangesAsync(ct);
        
        await context.RespondAsync<TaskCreateSuccess>(new
        {
            TaskId = entityTask.Id,
            Order = entityTask.TaskOrder
        });
    }
}