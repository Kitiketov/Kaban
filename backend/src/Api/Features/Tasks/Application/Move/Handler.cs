using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Move;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Tasks.Application.Common;
using Kaban.Api.Features.Tasks.Application.Common.Dto;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Move;

public class Handler(IRequestClient<MoveTaskRequest> requestClient)
    : IRequestHandler<MoveTaskCommand, TaskResponseDto>
{
    public async Task<TaskResponseDto> Handle(MoveTaskCommand request, CancellationToken ct)
    {
        var moveTaskRequest = new MoveTaskRequest(request.TaskId, request.ColumnId, request.PrevTaskId, request.NextTaskId);
        var response = await requestClient.GetResponse<
            TaskSuccess, 
            ITaskFailed>(moveTaskRequest, ct);
        
        var taskData = response.EnsureSuccess(); 
        return TaskResponseDto.FromSuccess(taskData);
    }
}