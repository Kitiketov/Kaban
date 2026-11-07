using Contracts.Features.Tasks.Delete;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;

namespace Kaban.Worker.Features.Tasks;

public class TaskDeleteConsumer(
    KabanDatabaseContext dbContext,
    ILogger<TaskCreateConsumer> logger) : IConsumer<DeleteTaskRequest>
{
    public Task Consume(ConsumeContext<DeleteTaskRequest> context)
    {
        throw new NotImplementedException();
    }
}