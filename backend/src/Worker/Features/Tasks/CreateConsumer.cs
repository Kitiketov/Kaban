using Contracts.Features.Tasks.Create;
using MassTransit;

namespace Kaban.Worker.Features.Tasks;

public class TaskCreateConsumer(
    ITaskService taskService,
    ILogger<TaskCreateConsumer> logger) : IConsumer<CreateTaskRequest>
{
    public async Task Consume(ConsumeContext<CreateTaskRequest> context)
    {
        var ct = context.CancellationToken;
        var message = context.Message;

        logger.LogInformation("Creating task '{Name}' in column {ColumnId}", message.Name, message.ColumnId);

        var result = await taskService.Create(message, ct);

        await result.Match(
            success => context.RespondAsync(success),
            colNotFound => context.RespondAsync(colNotFound),
            userNotFound => context.RespondAsync(userNotFound)
        );
    }
}