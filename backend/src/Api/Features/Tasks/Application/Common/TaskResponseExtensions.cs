using Contracts.Features.Tasks.Common;
using Kaban.Api.Common.Exceptions;
using MassTransit;

namespace Kaban.Api.Features.Tasks.Application.Common;

public static class TaskResponseExtensions
{
    public static void ThrowIfFailed(this Response<ITaskFailed> failed) => throw failed.Message switch
    {
        TaskNotFound e => new NotFoundException($"{e.Message} TaskId: {e.TaskId}"),
        TaskColumnNotFound e => new NotFoundException($"{e.Message} ColumnId: {e.ColumnId} BoardId: {e.BoardId}"),
        TaskUserNotFound e => new NotFoundException($"{e.Message} UserId: {e.UserId}"),
        _ => new InvalidOperationException($"Незарегистрированная ошибка Worker")
    };

    public static TaskSuccess EnsureSuccess(this Response<TaskSuccess, ITaskFailed> response)
    {
        if (response.Is(out Response<TaskSuccess>? success))
            return success.Message;

        if (response.Is(out Response<ITaskFailed>? failed)) failed.ThrowIfFailed();

        throw new InvalidOperationException("Неизвестный ответ от обработчика очереди.");
    }
}