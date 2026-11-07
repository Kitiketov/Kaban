using Contracts.Features.Tasks.Move;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;

namespace Kaban.Worker.Features.Tasks;

public class TasksMoveConsumer(
    KabanDatabaseContext dbContext,
    ILogger<TaskCreateConsumer> logger) : IConsumer<MoveTaskRequest>
{
    public Task Consume(ConsumeContext<MoveTaskRequest> context)
    {
        throw new NotImplementedException();
    }
}