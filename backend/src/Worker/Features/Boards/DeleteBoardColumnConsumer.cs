using Contracts.Features.Boards.DeleteColumn;
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

        var deleted = false;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            await db.Database.AcquireResourceLockAsync(LockResource.BoardColumn, request.ColumnId, ct);

            var column = await db.BoardColumns.FirstOrDefaultAsync(x => x.Id == request.ColumnId, ct);
            if (column is not null)
            {
                db.Remove(column);
                await db.SaveChangesAsync(ct);
                deleted = true;
            }

            await tx.CommitAsync(ct);
        }

        await context.RespondAsync(new DeleteBoardColumnSuccess(deleted));

        logger.LogInformation("Delete column result for {ColumnId}: {Deleted}", request.ColumnId, deleted);
    }
}
