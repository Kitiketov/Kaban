using Contracts.Features.Boards;
using MassTransit;
using MediatR;
using System.Diagnostics;

namespace Kaban.Api.Features.Boards.Application.DeleteBoardColumn;

public sealed class DeleteBoardColumnHandler(
    IRequestClient<DeleteBoardColumnRequest> client,
    ILogger<DeleteBoardColumnHandler> logger) : IRequestHandler<DeleteBoardColumnCommand, bool>
{
    public async Task<bool> Handle(DeleteBoardColumnCommand command, CancellationToken ct)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
            { ["TraceId"] = Activity.Current?.TraceId.ToString(), ["ColumnId"] = command.ColumnId });
        
        logger.LogInformation("Sending DeleteBoardColumn request to Worker");
        
        var r = await client.GetResponse<BoardColumnDeleted>(new DeleteBoardColumnRequest(command.ColumnId), ct);
        
        logger.LogInformation("Worker column deletion result: {Deleted}", r.Message.Deleted);
        
        return r.Message.Deleted;
    }
}