using Kaban.Api.Features.Tasks.Application;
using MassTransit;

namespace Kaban.Api.Common;

public static class RequestClientRegistration
{
    public static void AddRequestClients(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddTasksRequestClients();
    }
}