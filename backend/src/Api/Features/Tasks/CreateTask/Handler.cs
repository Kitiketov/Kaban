using Contracts.Features.Tasks;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Tasks.CreateTask;

public sealed class CreateTaskHandler(
    IRequestClient<CreateTaskRequest> requestClient) : IRequestHandler<CreateTaskCommand, TaskCreateResponseDto>
{
    public async Task<TaskCreateResponseDto> Handle(CreateTaskCommand command, CancellationToken ct)
    {
        var taskEvent = new CreateTaskRequest(
            command.Name,
            command.Description,
            command.Priority,
            command.Deadline,
            command.KanbanId,
            command.UserId,
            command.ColumnId,
            command.TagIds
        );

        var response = await requestClient.GetResponse<TaskCreateSuccess>(taskEvent, ct);

        var message = response.Message;
        return new TaskCreateResponseDto(
            message.TaskId,
            command.Name,
            command.Description,
            command.Priority,
            command.Deadline,
            command.KanbanId,
            command.UserId,
            message.Order,
            command.ColumnId,
            command.TagIds);
    }
}