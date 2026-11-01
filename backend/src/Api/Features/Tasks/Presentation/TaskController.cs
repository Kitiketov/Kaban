using Kaban.Api.Features.Tasks.CreateTask;
using Kaban.Api.Features.Tasks.DTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kaban.Api.Features.Tasks.Presentation;

[ApiController]
[Route("api/tasks")]
public class TaskController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TaskCreateResponseDto>> Create(
        [FromBody] TaskCreateRequestDto request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateTaskCommand(request.Name,
                request.Description,  
                request.Priority,     
                request.Deadline,    
                request.BoardId,
                request.UserId,       
                request.ColumnId,     
                request.Order,        
                request.TagIds), ct);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }
}