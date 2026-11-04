using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;
using System.Diagnostics;

namespace Kaban.Api.Features.Boards.Application.RenameBoardColumn;

public sealed class RenameBoardColumnHandler(
    IRequestClient<RenameBoardColumnRequest> client,
    ILogger<RenameBoardColumnHandler> logger) : IRequestHandler<RenameBoardColumnCommand, BoardColumnDto>
{
    public async Task<BoardColumnDto> Handle(RenameBoardColumnCommand command, CancellationToken ct)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
            { ["TraceId"] = Activity.Current?.TraceId.ToString(), ["ColumnId"] = command.ColumnId });
        
        logger.LogInformation("Sending RenameBoardColumn request to Worker");
        
        var r = await client.GetResponse<BoardColumnRenamed>(
            new RenameBoardColumnRequest(command.ColumnId, command.Name), ct);
        
        logger.LogInformation("Worker renamed column {ColumnId}", r.Message.Id);
        
        return new BoardColumnDto(r.Message.Id, r.Message.Name, r.Message.Order);
    }
}