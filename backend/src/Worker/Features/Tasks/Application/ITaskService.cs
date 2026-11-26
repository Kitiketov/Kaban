using Contracts.Features.Tasks.Common;
using Contracts.Features.Tasks.Create;
using Contracts.Features.Tasks.Delete;
using Contracts.Features.Tasks.Move;
using Contracts.Features.Tasks.Update;
using OneOf;

namespace Kaban.Worker.Features.Tasks.Application;

public interface ITaskService
{
    Task<OneOf<TaskSuccess, TaskColumnNotFound, TaskUserNotFound>> Create(
        CreateTaskRequest request, CancellationToken ct);

    Task<OneOf<DeleteTaskSuccess, TaskNotFound>> Delete(DeleteTaskRequest request, CancellationToken ct);

    Task<OneOf<TaskSuccess, TaskNotFound, TaskColumnNotFound, TaskNotInColumn>> Move(MoveTaskRequest request,
        CancellationToken ct);

    Task<OneOf<TaskSuccess, TaskNotFound, TaskColumnNotFound, TaskUserNotFound>> Update(UpdateTaskRequest request,
        CancellationToken ct);
}