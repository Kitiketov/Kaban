using Contracts.Tasks;
using Kaban.Worker.Infrastructure.Database;
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
            TaskOrder = message.Order,
            Description = message.Description,
            Deadline = DateOnly.FromDateTime(message.Deadline),
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