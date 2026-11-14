using Kaban.Api.Features.Boards.Application.AddBoardColumn;
using Kaban.Api.Features.Boards.Application.DeleteBoardColumn;
using Kaban.Api.Features.Boards.Application.RenameBoardColumn;
using Kaban.Api.Features.Boards.DTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kaban.Api.Features.Boards.Presentation;

[ApiController]
[Tags("Board Columns")]
[Route("api")]
public sealed class BoardColumnsController(IMediator mediator) : ControllerBase
{
    [HttpPost("kanbans/{kanbanId:int}/columns")]
    public Task<BoardColumnDto> AddColumn(int kanbanId, CreateBoardColumnDto request, CancellationToken ct) =>
        mediator.Send(new AddBoardColumnCommand(kanbanId, request.Name), ct);

    [HttpPut("columns/{columnId:int}")]
    public Task<BoardColumnDto> RenameColumn(int columnId, RenameBoardColumnDto request, CancellationToken ct) =>
        mediator.Send(new RenameBoardColumnCommand(columnId, request.Name), ct);

    [HttpDelete("columns/{columnId:int}")]
    public async Task<IActionResult> DeleteColumn(int columnId, CancellationToken ct) =>
        await mediator.Send(new DeleteBoardColumnCommand(columnId), ct) ? NoContent() : NotFound();
}
