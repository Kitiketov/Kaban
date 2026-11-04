using Contracts.Features.Boards;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Boards;

public sealed class DeleteBoardColumnConsumer(KabanDatabaseContext db, ILogger<DeleteBoardColumnConsumer> logger)
    : IConsumer<DeleteBoardColumnRequest>
{
    public async Task Consume(ConsumeContext<DeleteBoardColumnRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;

        var column = await db.BoardColumns.FirstOrDefaultAsync(x => x.Id == request.ColumnId, ct);
        if (column is null)
        {
            await context.RespondAsync(new BoardColumnDeleted(false));
            logger.LogInformation("Delete column result for {ColumnId}: {Deleted}", request.ColumnId, false);
            return;
        }

        db.Remove(column);
        await db.SaveChangesAsync(ct);
        await context.RespondAsync(new BoardColumnDeleted(true));

        logger.LogInformation("Delete column result for {ColumnId}: {Deleted}", request.ColumnId, true);
    }
}