using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Create;
using Kaban.Api.Features.Tasks.Application.Common;
using Kaban.Api.Features.Tasks.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Create;

public sealed class CreateTaskHandler(IRequestClient<CreateTaskRequest> requestClient)
    : IRequestHandler<CreateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(CreateTaskCommand command, CancellationToken ct)
    {
        var createTaskEvent = new CreateTaskRequest(
            command.Name,
            command.Description,
            command.Priority,
            command.Deadline,
            command.KanbanId,
            command.UserId,
            command.ColumnId,
            command.TagIds
        );

        var response = await requestClient.GetResponse<
            TaskSuccess,
            ITaskFailed>(createTaskEvent, ct);

        return TaskDto.FromSuccess(response.EnsureSuccess(), command.KanbanId, command.UserId);
    }
}