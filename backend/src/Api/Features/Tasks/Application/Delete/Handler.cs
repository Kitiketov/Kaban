using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Delete;
using MassTransit;
using MediatR;
using Kaban.Api.Features.Tasks.Application.Common;
namespace Kaban.Api.Features.Tasks.Application.Delete;

public class Handler(IRequestClient<DeleteTaskRequest> requestClient) : IRequestHandler<DeleteTaskCommand>
{
    public async Task Handle(DeleteTaskCommand request, CancellationToken ct)
    {
        var deleteTaskRequest = new DeleteTaskRequest(request.TaskId);
        var response = await requestClient.GetResponse<DeleteTaskSuccess, ITaskFailed>(deleteTaskRequest, ct);
        if (response.Is(out Response<ITaskFailed>? failed))
            failed.ThrowIfFailed();
    }
}