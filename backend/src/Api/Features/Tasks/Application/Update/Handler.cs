using Contracts.Features.Tasks.Create;
using Contracts.Features.Tasks.Update;
using Kaban.Api.Features.Tasks.Application.Common.Dto;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Update;

public class Handler(IRequestClient<UpdateTaskRequest> requestClient)
    : IRequestHandler<UpdateTaskCommand, TaskResponseDto>
{
    public Task<TaskResponseDto> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}