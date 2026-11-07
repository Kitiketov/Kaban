using Contracts.Features.Tasks.Update;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;

namespace Kaban.Worker.Features.Tasks;

public class TasksUpdateConsumer(
    KabanDatabaseContext dbContext,
    ILogger<TaskCreateConsumer> logger) : IConsumer<UpdateTaskRequest>
{
    public Task Consume(ConsumeContext<UpdateTaskRequest> context)
    {
        throw new NotImplementedException();
    }
}