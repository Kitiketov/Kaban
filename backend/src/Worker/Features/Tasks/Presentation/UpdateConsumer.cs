using Contracts.Features.Tasks.Update;
using Kaban.Worker.Features.Tasks.Application;
using MassTransit;

namespace Kaban.Worker.Features.Tasks.Presentation;

public class TasksUpdateConsumer(
    ITaskService taskService,
    ILogger<TasksUpdateConsumer> logger) : IConsumer<UpdateTaskRequest>
{
    public async Task Consume(ConsumeContext<UpdateTaskRequest> context)
    {
        var ct = context.CancellationToken;
        var message = context.Message;
        
        var moveTaskResult = await taskService.Update(message, ct);
        
        await moveTaskResult.Match(                                                                                                                                                       
            success      => context.RespondAsync(success),                                                                                                                        
            taskNotFound  => context.RespondAsync(taskNotFound),                                                                                                               
            taskColumnNotFound  => context.RespondAsync(taskColumnNotFound) ,                                                                                                              
            taskNotInColumn  => context.RespondAsync(taskNotInColumn)                                                                                                               
        );                                                                                                            
    }
}