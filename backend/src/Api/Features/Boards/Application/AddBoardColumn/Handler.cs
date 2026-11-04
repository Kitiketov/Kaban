using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.AddBoardColumn;

public sealed class AddBoardColumnHandler(
    IRequestClient<AddBoardColumnRequest> client) : IRequestHandler<AddBoardColumnCommand, BoardColumnDto>
{
    public async Task<BoardColumnDto> Handle(AddBoardColumnCommand command, CancellationToken ct)
    {
        var r = await client.GetResponse<BoardColumnAdded>(new AddBoardColumnRequest(command.BoardId, command.Name),
            ct);

        return new BoardColumnDto(r.Message.Id, r.Message.Name, r.Message.Order);
    }
}
