using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.GetBoard;

public sealed class GetBoardHandler(IRequestClient<GetBoardRequest> client)
    : IRequestHandler<GetBoardQuery, BoardDetailsDto>
{
    public async Task<BoardDetailsDto> Handle(GetBoardQuery query, CancellationToken ct)
    {
        var response = await client.GetResponse<BoardDetails>(new GetBoardRequest(query.BoardId), ct);
        return new BoardDetailsDto(response.Message.Id, response.Message.ProjectId, response.Message.Name,
            response.Message.Columns.Select(x => new BoardColumnDto(x.Id, x.Name, x.Order)).ToArray());
    }
}
