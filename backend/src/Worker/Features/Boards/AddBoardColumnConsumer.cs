using Contracts.Features.Boards;
using Kaban.Worker.Infrastructure.Database;
using Kaban.Worker.Infrastructure.Database.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Boards;

public sealed class AddBoardColumnConsumer(KabanDatabaseContext db, ILogger<AddBoardColumnConsumer> logger)
    : IConsumer<AddBoardColumnRequest>
{
    public async Task Consume(ConsumeContext<AddBoardColumnRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;

        if (!await db.Boards.AnyAsync(x => x.Id == request.BoardId, ct))
            throw new InvalidOperationException("Board not found.");

        var order = (await db.BoardColumns.Where(x => x.BoardId == request.BoardId)
            .MaxAsync(x => (int?)x.BoardOrder, ct) ?? -1) + 1;

        var column = new BoardColumns
        {
            BoardId = request.BoardId,
            ColumnName = request.Name,
            BoardOrder = order
        };

        db.Add(column);

        await db.SaveChangesAsync(ct);
        await context.RespondAsync(
            new BoardColumnAdded(column.Id, column.BoardId, column.ColumnName, column.BoardOrder));

        logger.LogInformation("Added column {ColumnId} to board {BoardId} at order {Order}", column.Id,
            column.BoardId, order);
    }
}