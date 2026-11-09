using Contracts.Features.Boards;
using Contracts.Features.Tasks.Create;
using Contracts.Features.Tasks.Delete;
using Contracts.Features.Tasks.Move;
using Contracts.Features.Tasks.Update;
using MassTransit;

namespace Kaban.Api.Common;

public static class MassTransitRequestClientRegistration
{
    private static readonly RequestTimeout DefaultRequestTimeout = RequestTimeout.After(s: 30);

    public static void AddKabanRequestClients(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddRequestClient<CreateTaskRequest>(DefaultRequestTimeout);
        configurator.AddRequestClient<DeleteTaskRequest>(DefaultRequestTimeout);
        configurator.AddRequestClient<MoveTaskRequest>(DefaultRequestTimeout);
        configurator.AddRequestClient<UpdateTaskRequest>(DefaultRequestTimeout);

        configurator.AddRequestClient<CreateBoardRequest>(DefaultRequestTimeout);
        configurator.AddRequestClient<ListBoardsRequest>(DefaultRequestTimeout);
        configurator.AddRequestClient<GetBoardRequest>(DefaultRequestTimeout);
        configurator.AddRequestClient<RenameBoardRequest>(DefaultRequestTimeout);
        configurator.AddRequestClient<DeleteBoardRequest>(DefaultRequestTimeout);
        configurator.AddRequestClient<AddBoardColumnRequest>(DefaultRequestTimeout);
        configurator.AddRequestClient<RenameBoardColumnRequest>(DefaultRequestTimeout);
        configurator.AddRequestClient<DeleteBoardColumnRequest>(DefaultRequestTimeout);
    }
}
