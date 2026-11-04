using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;
using System.Diagnostics;

namespace Kaban.Api.Features.Boards.Application.CreateBoard;

public sealed class CreateBoardHandler(IRequestClient<CreateBoardRequest> client, ILogger<CreateBoardHandler> logger)
    : IRequestHandler<CreateBoardCommand, BoardDto>
{
    public async Task<BoardDto> Handle(CreateBoardCommand command, CancellationToken ct)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
            { ["TraceId"] = Activity.Current?.TraceId.ToString(), ["ProjectId"] = command.ProjectId });
        
        logger.LogInformation("Sending CreateBoard request to Worker");
        
        var response =
            await client.GetResponse<BoardCreated>(new CreateBoardRequest(command.ProjectId, command.Name), ct);
        
        logger.LogInformation("Worker created board {BoardId}", response.Message.Id);
        
        return new BoardDto(response.Message.Id, response.Message.ProjectId, response.Message.Name);
    }
}