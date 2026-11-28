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

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.AcquireResourceLockAsync(LockResource.BoardColumn, request.ColumnId, ct);

        var column = await db.BoardColumns.FirstOrDefaultAsync(x => x.Id == request.ColumnId, ct);
        if (column is null)
        {
            await context.RespondAsync(new DeleteBoardColumnSuccess(false));
            logger.LogInformation("Delete column result for {ColumnId}: {Deleted}", request.ColumnId, false);
            return;
        }

        db.Remove(column);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await context.RespondAsync(new DeleteBoardColumnSuccess(true));

        logger.LogInformation("Delete column result for {ColumnId}: {Deleted}", request.ColumnId, true);
    }
}