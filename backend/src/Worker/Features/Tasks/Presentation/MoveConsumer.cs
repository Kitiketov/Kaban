using Contracts.Features.Tasks.Move;
using Kaban.Worker.Features.Tasks.Application;
using MassTransit;

namespace Kaban.Worker.Features.Tasks.Presentation;

public class TasksMoveConsumer(
    ITaskService taskService,
    ILogger<TasksMoveConsumer> logger) : IConsumer<MoveTaskRequest>
{
    public async Task Consume(ConsumeContext<MoveTaskRequest> context)
    {
        var ct = context.CancellationToken;
        var message = context.Message;
        logger.LogInformation("Moving task '{TaskId}'", message.TaskId);
        
        var moveTaskResult = await taskService.Move(message, ct);
        
        await moveTaskResult.Match(                                                                                                                                                       
            success      => context.RespondAsync(success),                                                                                                                        
            taskNotFound  => context.RespondAsync(taskNotFound),                                                                                                               
            taskColumnNotFound  => context.RespondAsync(taskColumnNotFound) ,                                                                                                              
            taskNotInColumn  => context.RespondAsync(taskNotInColumn)                                                                                                               
        );  
    }
}