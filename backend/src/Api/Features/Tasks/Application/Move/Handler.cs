using Contracts.Features.Tasks.Create;
using Contracts.Features.Tasks.Move;
using Kaban.Api.Features.Tasks.Application.Common.Dto;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Move;

public class Handler(IRequestClient<MoveTaskRequest> requestClient)
    : IRequestHandler<MoveTaskCommand, TaskResponseDto>
{
    public Task<TaskResponseDto> Handle(MoveTaskCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}