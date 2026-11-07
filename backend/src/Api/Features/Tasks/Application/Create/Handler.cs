using Contracts.Features.Tasks;
using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Create;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Tasks.Application.Common.Dto;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Create;

public sealed class CreateTaskHandler(IRequestClient<CreateTaskRequest> requestClient)
    : IRequestHandler<CreateTaskCommand, TaskResponseDto>
{
    public async Task<TaskResponseDto> Handle(CreateTaskCommand command, CancellationToken ct)
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

        var response = await requestClient.GetResponse<
            CreateTaskSuccess,
            CreateTaskColumnNotFound,
            CreateTaskUserNotFound>(taskEvent, ct);

        if (response.Is(out Response<CreateTaskColumnNotFound>? columnNotFound))
            throw new NotFoundException(columnNotFound.Message.Message);

        if (response.Is(out Response<CreateTaskUserNotFound>? userNotFound))
            throw new NotFoundException(userNotFound.Message.Message);

        if (!response.Is(out Response<CreateTaskSuccess>? taskSuccess))
            throw new InvalidOperationException("Неизвестный ответ от обработчика очереди.");

        var message = taskSuccess.Message;
        return new TaskResponseDto(
            message.Id,
            message.Name,
            message.Description,
            message.Priority,
            message.Deadline,
            command.KanbanId,
            command.UserId,
            message.Order,
            message.ColumnId,
            command.TagIds);
    }
}