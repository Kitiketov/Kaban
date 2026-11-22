using Contracts.Features.Tasks.Delete;
using MassTransit;

namespace Kaban.Worker.Features.Tasks;

public class TaskDeleteConsumer(
    ITaskService taskService,
    ILogger<TaskDeleteConsumer> logger) : IConsumer<DeleteTaskRequest>
{
    public async Task Consume(ConsumeContext<DeleteTaskRequest> context)
    {
        var ct = context.CancellationToken;
        var message = context.Message;
        logger.LogInformation("Deleting task '{TaskId}'", message.TaskId);

        var deleteTaskResult = await taskService.Delete(message, ct);

        await deleteTaskResult.Match(
            success => context.RespondAsync(success),
            taskNotFound => context.RespondAsync(taskNotFound)
        );
    }
}