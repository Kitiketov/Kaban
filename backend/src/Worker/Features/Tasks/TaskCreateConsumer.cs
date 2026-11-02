using Contracts.Features.Tasks;
using Kaban.Worker.Infrastructure.Database;
using Kaban.Worker.Infrastructure.Database.Domain;
using MassTransit;

namespace Kaban.Worker.Features.Tasks;

public class TaskCreateConsumer(
    KabanDatabaseContext dbContext,
    ILogger<TaskCreateConsumer> logger) : IConsumer<TaskCreateEvent>
{
    public async Task Consume(ConsumeContext<TaskCreateEvent> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Handled event {EventType}: {Name}",
            nameof(TaskCreateEvent),
            message.Name);
        
        var entityTask = new Infrastructure.Database.Domain.Tasks
        {
            Name = message.Name,
            Created = DateTime.UtcNow,
            Description = string.IsNullOrEmpty(message.Description) ? null : message.Description,
            Deadline = message.Deadline == default ? message.Deadline : null,
            TaskOrder = message.Order,
            Priority = (PriorityEnum)message.Priority,
            BoardColumnId = message.ColumnId,
        };
        
        await dbContext.AddAsync(entityTask);
        await dbContext.SaveChangesAsync();
        
        await context.RespondAsync<TaskCreateSuccess>(new
        {
            TaskId = entityTask.Id,
        });
    }
}