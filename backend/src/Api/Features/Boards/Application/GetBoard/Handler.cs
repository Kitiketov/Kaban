using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;
using System.Diagnostics;

namespace Kaban.Api.Features.Boards.Application.GetBoard;

public sealed class GetBoardHandler(IRequestClient<GetBoardRequest> client, ILogger<GetBoardHandler> logger)
    : IRequestHandler<GetBoardQuery, BoardDetailsDto>
{
    public async Task<BoardDetailsDto> Handle(GetBoardQuery query, CancellationToken ct)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
            { ["TraceId"] = Activity.Current?.TraceId.ToString(), ["BoardId"] = query.BoardId });
        
        logger.LogInformation("Sending GetBoard request to Worker");
        
        var response = await client.GetResponse<BoardDetails>(new GetBoardRequest(query.BoardId), ct);
        
        logger.LogInformation("Worker returned board with {ColumnCount} columns", response.Message.Columns.Count);
        
        return new BoardDetailsDto(response.Message.Id, response.Message.ProjectId, response.Message.Name,
            response.Message.Columns.Select(x => new BoardColumnDto(x.Id, x.Name, x.Order)).ToArray());
    }
}