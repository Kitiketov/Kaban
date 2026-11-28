using Contracts.Features.Boards.List;
using Kaban.Api.Features.Boards.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.ListBoards;

public sealed class ListBoardsHandler(IRequestClient<ListBoardsRequest> client)
    : IRequestHandler<ListBoardsQuery, BoardListDto>
{
    public async Task<BoardListDto> Handle(ListBoardsQuery query, CancellationToken ct)
    {
        var response = await client.GetResponse<ListBoardsSuccess>(new ListBoardsRequest(query.ProjectId), ct);
        return new BoardListDto(response.Message.Boards.Select(x => new BoardDto(x.Id, x.ProjectId, x.Name)).ToArray());
    }
}
