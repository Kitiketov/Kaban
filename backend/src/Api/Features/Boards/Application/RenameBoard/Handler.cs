using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;
using System.Diagnostics;

namespace Kaban.Api.Features.Boards.Application.RenameBoard;

public sealed class RenameBoardHandler(IRequestClient<RenameBoardRequest> client, ILogger<RenameBoardHandler> logger)
    : IRequestHandler<RenameBoardCommand, BoardDto>
{
    public async Task<BoardDto> Handle(RenameBoardCommand command, CancellationToken ct)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
            { ["TraceId"] = Activity.Current?.TraceId.ToString(), ["BoardId"] = command.BoardId });

        logger.LogInformation("Sending RenameBoard request to Worker");

        var r = await client.GetResponse<BoardRenamed>(new RenameBoardRequest(command.BoardId, command.Name), ct);

        logger.LogInformation("Worker renamed board {BoardId}", r.Message.Id);

        return new BoardDto(r.Message.Id, r.Message.ProjectId, r.Message.Name);
    }
}