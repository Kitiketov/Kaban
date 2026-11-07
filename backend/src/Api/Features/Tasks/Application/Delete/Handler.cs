using Contracts.Features.Tasks.Create;
using Contracts.Features.Tasks.Delete;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Delete;

public class Handler(IRequestClient<DeleteTaskRequest> requestClient) : IRequestHandler<DeleteTaskCommand>
{
    public Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}