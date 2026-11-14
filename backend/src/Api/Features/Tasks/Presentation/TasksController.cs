using Kaban.Api.Features.Tasks.Application.Common.Dto;
using Kaban.Api.Features.Tasks.Application.Create;
using Kaban.Api.Features.Tasks.Application.Delete;
using Kaban.Api.Features.Tasks.Application.Move;
using Kaban.Api.Features.Tasks.Application.Update;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kaban.Api.Features.Tasks.Presentation;

[ApiController]
[Tags("Tasks")]
[Route("api")]
public sealed class TasksController(IMediator mediator) : ControllerBase
{
    [HttpPost("kanbans/{kanbanId:int}/tasks")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskResponseDto>> Create(
        int kanbanId,
        [FromBody] TaskRequestDto request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateTaskCommand(
                request.Name,
                request.Description,
                request.Priority,
                request.Deadline,
                kanbanId,
                request.UserId,
                request.ColumnId,
                request.TagIds), ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("tasks/{taskId:int}")]
    public async Task<ActionResult<TaskResponseDto>> Update(
        int taskId,
        [FromBody] TaskRequestDto request,
        CancellationToken ct)
    {
        return await mediator.Send(
            new UpdateTaskCommand(
                taskId,
                request.Name,
                request.Description,
                request.Priority,
                request.Deadline,
                request.UserId,
                request.ColumnId,
                request.TagIds), ct);
    }

    [HttpPatch("tasks/{taskId:int}")]
    public async Task<ActionResult<TaskResponseDto>> Move(
        int taskId,
        [FromBody] TaskMoveRequestDto request,
        CancellationToken ct)
    {
        return await mediator.Send(
            new MoveTaskCommand(
                taskId,
                request.ColumnId,
                request.PrevTaskId,
                request.NextTaskId), ct);
    }

    [HttpDelete("tasks/{taskId:int}")]
    public async Task<ActionResult> Delete(int taskId, CancellationToken ct)
    {
        await mediator.Send(new DeleteTaskCommand(taskId), ct);
        return Ok();
    }
}
