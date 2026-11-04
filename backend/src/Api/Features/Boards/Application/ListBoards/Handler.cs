using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;
using System.Diagnostics;

namespace Kaban.Api.Features.Boards.Application.ListBoards;

public sealed class ListBoardsHandler(IRequestClient<ListBoardsRequest> client, ILogger<ListBoardsHandler> logger)
    : IRequestHandler<ListBoardsQuery, BoardListDto>
{
    public async Task<BoardListDto> Handle(ListBoardsQuery query, CancellationToken ct)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
            { ["TraceId"] = Activity.Current?.TraceId.ToString(), ["ProjectId"] = query.ProjectId });
        
        logger.LogInformation("Sending ListBoards request to Worker");
        
        var response = await client.GetResponse<BoardListResult>(new ListBoardsRequest(query.ProjectId), ct);
        
        logger.LogInformation("Worker returned {BoardCount} boards", response.Message.Boards.Count);
        
        return new BoardListDto(response.Message.Boards.Select(x => new BoardDto(x.Id, x.ProjectId, x.Name)).ToArray());
    }
}