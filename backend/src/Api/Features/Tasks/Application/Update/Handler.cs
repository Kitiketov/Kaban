using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Create;
using Contracts.Features.Tasks.Update;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Tasks.Application.Common;
using Kaban.Api.Features.Tasks.Application.Common.Dto;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Update;

public class Handler(IRequestClient<UpdateTaskRequest> requestClient)
    : IRequestHandler<UpdateTaskCommand, TaskResponseDto>
{
    public async Task<TaskResponseDto> Handle(UpdateTaskCommand command, CancellationToken ct)
    {
        var updateTaskRequest =  new UpdateTaskRequest(
            command.TaskId,
            command.Name,
            command.Description,
            command.Priority,
            command.Deadline,
            command.UserId,
            command.ColumnId,
            command.TagIds
        );
        var response = await requestClient.GetResponse<
            TaskSuccess, 
            ITaskFailed>(updateTaskRequest, ct);

        var taskData = response.EnsureSuccess();
        return TaskResponseDto.FromSuccess(taskData);
    }
}