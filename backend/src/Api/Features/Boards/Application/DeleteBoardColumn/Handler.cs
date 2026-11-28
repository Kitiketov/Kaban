using Contracts.Features.Boards.DeleteColumn;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.DeleteBoardColumn;

public sealed class DeleteBoardColumnHandler(
    IRequestClient<DeleteBoardColumnRequest> client) : IRequestHandler<DeleteBoardColumnCommand, bool>
{
    public async Task<bool> Handle(DeleteBoardColumnCommand command, CancellationToken ct)
    {
        var r = await client.GetResponse<DeleteBoardColumnSuccess>(new DeleteBoardColumnRequest(command.ColumnId), ct);
        return r.Message.Deleted;
    }
}