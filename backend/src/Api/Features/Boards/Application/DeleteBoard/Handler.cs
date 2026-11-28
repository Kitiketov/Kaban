using Contracts.Features.Boards.Delete;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.DeleteBoard;

public sealed class DeleteBoardHandler(IRequestClient<DeleteBoardRequest> client)
    : IRequestHandler<DeleteBoardCommand, bool>
{
    public async Task<bool> Handle(DeleteBoardCommand command, CancellationToken ct)
    {
        var r = await client.GetResponse<DeleteBoardSuccess>(new DeleteBoardRequest(command.BoardId), ct);
        return r.Message.Deleted;
    }
}
