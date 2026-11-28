using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Delete;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Tasks;

public sealed class TaskDeleteConsumer(
    KabanDatabaseContext dbContext,
    ILogger<TaskDeleteConsumer> logger) : IConsumer<DeleteTaskRequest>
{
    public async Task Consume(ConsumeContext<DeleteTaskRequest> context)
    {
        var ct = context.CancellationToken;
        var request = context.Message;

        logger.LogInformation("Deleting task '{TaskId}'", request.TaskId);

        var affected = await dbContext.Tasks.Where(t => t.Id == request.TaskId).ExecuteDeleteAsync(ct);
        if (affected == 0)
        {
            await context.RespondAsync(new TaskNotFound(request.TaskId));
            return;
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Deleted task '{TaskId}'", request.TaskId);
        await context.RespondAsync(new DeleteTaskSuccess(request.TaskId));
    }
}
