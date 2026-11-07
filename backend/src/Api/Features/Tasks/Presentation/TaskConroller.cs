using Kaban.Api.Features.Tasks.Application.Common.Dto;
using Kaban.Api.Features.Tasks.Application.Delete;
using Kaban.Api.Features.Tasks.Application.Move;
using Kaban.Api.Features.Tasks.Application.Update;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMoveRequestDto = Kaban.Api.Features.Tasks.Application.Update.TaskMoveRequestDto;

namespace Kaban.Api.Features.Tasks.Presentation;

[ApiController]
[Route("api/tasks/{taskId:int}")]
public class TaskController(IMediator mediator) : ControllerBase
{
    [HttpPut]
    public async Task<ActionResult<TaskResponseDto>> Update([FromBody] TaskRequestDto request, int taskId,
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

    [HttpPatch]
    public async Task<ActionResult<TaskResponseDto>> Move([FromBody] TaskMoveRequestDto request, int taskId,
        CancellationToken ct)
    {
        return await mediator.Send(
            new MoveTaskCommand(
                taskId,
                request.ColumnId,
                request.Order), ct);
    }

    [HttpDelete]
    public async Task<ActionResult> Delete(int taskId, CancellationToken ct)
    {
        await mediator.Send(new DeleteTaskCommand(taskId), ct);
        return Ok();
    }
}