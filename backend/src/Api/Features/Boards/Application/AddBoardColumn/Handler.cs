using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;
using System.Diagnostics;

namespace Kaban.Api.Features.Boards.Application.AddBoardColumn;

public sealed class AddBoardColumnHandler(
    IRequestClient<AddBoardColumnRequest> client,
    ILogger<AddBoardColumnHandler> logger) : IRequestHandler<AddBoardColumnCommand, BoardColumnDto>
{
    public async Task<BoardColumnDto> Handle(AddBoardColumnCommand command, CancellationToken ct)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
            { ["TraceId"] = Activity.Current?.TraceId.ToString(), ["BoardId"] = command.BoardId });

        logger.LogInformation("Sending AddBoardColumn request to Worker");

        var r = await client.GetResponse<BoardColumnAdded>(new AddBoardColumnRequest(command.BoardId, command.Name),
            ct);

        logger.LogInformation("Worker added board column {ColumnId} at order {Order}", r.Message.Id, r.Message.Order);

        return new BoardColumnDto(r.Message.Id, r.Message.Name, r.Message.Order);
    }
}