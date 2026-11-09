using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.RenameBoardColumn;

public sealed class RenameBoardColumnHandler(
    IRequestClient<RenameBoardColumnRequest> client) : IRequestHandler<RenameBoardColumnCommand, BoardColumnDto>
{
    public async Task<BoardColumnDto> Handle(RenameBoardColumnCommand command, CancellationToken ct)
    {
        var r = await client.GetResponse<BoardColumnRenamed>(
            new RenameBoardColumnRequest(command.ColumnId, command.Name), ct);
        return new BoardColumnDto(r.Message.Id, r.Message.Name, r.Message.Order);
    }
}