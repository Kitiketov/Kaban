using Contracts.Features.Tasks.Create;
using Contracts.Features.Tasks.Delete;
using Contracts.Features.Tasks.Move;
using Contracts.Features.Tasks.Update;
using MassTransit;

namespace Kaban.Api.Features.Tasks.Application;

public static class TasksRequestClientRegistration
{
    public static void AddTasksRequestClients(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddRequestClient<CreateTaskRequest>();
        configurator.AddRequestClient<UpdateTaskRequest>();
        configurator.AddRequestClient<MoveTaskRequest>();
        configurator.AddRequestClient<DeleteTaskRequest>();
    }
}