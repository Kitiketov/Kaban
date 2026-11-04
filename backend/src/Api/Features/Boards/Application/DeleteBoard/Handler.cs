using Contracts.Features.Boards;
using MassTransit;
using MediatR;
using System.Diagnostics;

namespace Kaban.Api.Features.Boards.Application.DeleteBoard;

public sealed class DeleteBoardHandler(IRequestClient<DeleteBoardRequest> client, ILogger<DeleteBoardHandler> logger)
    : IRequestHandler<DeleteBoardCommand, bool>
{
    public async Task<bool> Handle(DeleteBoardCommand command, CancellationToken ct)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
            { ["TraceId"] = Activity.Current?.TraceId.ToString(), ["BoardId"] = command.BoardId });
        
        logger.LogInformation("Sending DeleteBoard request to Worker");
        
        var r = await client.GetResponse<BoardDeleted>(new DeleteBoardRequest(command.BoardId), ct);
        
        logger.LogInformation("Worker deletion result: {Deleted}", r.Message.Deleted);
        
        return r.Message.Deleted;
    }
}