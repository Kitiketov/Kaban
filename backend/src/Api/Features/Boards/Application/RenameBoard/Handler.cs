using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.RenameBoard;

public sealed class RenameBoardHandler(IRequestClient<RenameBoardRequest> client)
    : IRequestHandler<RenameBoardCommand, BoardDto>
{
    public async Task<BoardDto> Handle(RenameBoardCommand command, CancellationToken ct)
    {
        var r = await client.GetResponse<BoardRenamed>(new RenameBoardRequest(command.BoardId, command.Name), ct);

        return new BoardDto(r.Message.Id, r.Message.ProjectId, r.Message.Name);
    }
}
