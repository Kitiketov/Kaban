using Kaban.Api.Features.Boards.Application.CreateBoard;
using Kaban.Api.Features.Boards.Application.DeleteBoard;
using Kaban.Api.Features.Boards.Application.DTO;
using Kaban.Api.Features.Boards.Application.GetBoard;
using Kaban.Api.Features.Boards.Application.ListBoards;
using Kaban.Api.Features.Boards.Application.RenameBoard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kaban.Api.Features.Boards.Presentation;

public sealed record CreateBoardRequest(string Name);
public sealed record RenameBoardRequest(string Name);

[ApiController]
[Tags("Boards")]
[Route("api")]
public sealed class BoardsController(IMediator mediator) : ControllerBase
{
    [HttpPost("projects/{projectId:int}/kanbans")]
    public Task<BoardDto> Create(int projectId, CreateBoardRequest request, CancellationToken ct) =>
        mediator.Send(new CreateBoardCommand(projectId, request.Name), ct);

    [HttpGet("projects/{projectId:int}/kanbans")]
    public Task<BoardListDto> List(int projectId, CancellationToken ct) =>
        mediator.Send(new ListBoardsQuery(projectId), ct);

    [HttpGet("kanbans/{kanbanId:int}")]
    public Task<BoardDetailsDto> Get(int kanbanId, CancellationToken ct) =>
        mediator.Send(new GetBoardQuery(kanbanId), ct);

    [HttpPut("kanbans/{kanbanId:int}")]
    public Task<BoardDto> Rename(int kanbanId, RenameBoardRequest request, CancellationToken ct) =>
        mediator.Send(new RenameBoardCommand(kanbanId, request.Name), ct);

    [HttpDelete("kanbans/{kanbanId:int}")]
    public async Task<IActionResult> Delete(int kanbanId, CancellationToken ct) =>
        await mediator.Send(new DeleteBoardCommand(kanbanId), ct) ? NoContent() : NotFound();
}
