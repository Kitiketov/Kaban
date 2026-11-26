using Contracts.Features.Boards.Get;
using Kaban.Api.Features.Boards.Application.DTO;
using Kaban.Api.Features.Tasks.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.GetBoard;

public sealed class GetBoardHandler(IRequestClient<GetBoardRequest> client)
    : IRequestHandler<GetBoardQuery, BoardDetailsDto>
{
    public async Task<BoardDetailsDto> Handle(GetBoardQuery query, CancellationToken ct)
    {
        var response = await client.GetResponse<GetBoardSuccess>(new GetBoardRequest(query.BoardId), ct);
        return new BoardDetailsDto(response.Message.Id, response.Message.ProjectId, response.Message.Name,
        [
            .. response.Message.Columns.Select(x => new BoardColumnDetailsDto(x.Id, x.Name, x.Order,
                [.. x.Tasks.Select(t => TaskDto.FromSuccess(t))]))
        ]);
    }
}