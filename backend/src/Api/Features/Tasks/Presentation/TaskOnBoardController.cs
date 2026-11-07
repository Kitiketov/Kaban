using Kaban.Api.Features.Tasks.Application;
using Kaban.Api.Features.Tasks.Application.Common.Dto;
using Kaban.Api.Features.Tasks.Application.Create;
using MassTransit.Futures.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kaban.Api.Features.Tasks.Presentation;

[ApiController]
[Route("api/kanbans/{kanbanId:int}/tasks")]
public class TaskOnBoardController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TaskResponseDto>> Create(
        [FromBody] TaskRequestDto request, int kanbanId, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateTaskCommand(request.Name,
                request.Description,
                request.Priority,
                request.Deadline,
                kanbanId,
                request.UserId,
                request.ColumnId,
                request.TagIds), ct);
        return CreatedAtAction(nameof(Create), result);
    }
}