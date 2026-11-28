using Contracts.Features.Boards.Delete;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Boards;

public sealed class DeleteBoardConsumer(KabanDatabaseContext db, ILogger<DeleteBoardConsumer> logger)
    : IConsumer<DeleteBoardRequest>
{
    public async Task Consume(ConsumeContext<DeleteBoardRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;

        var deleted = false;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            await db.Database.AcquireResourceLockAsync(LockResource.Board, request.BoardId, ct);

            var board = await db.Boards.FirstOrDefaultAsync(x => x.Id == request.BoardId, ct);
            if (board is not null)
            {
                db.Remove(board);
                await db.SaveChangesAsync(ct);
                deleted = true;
            }

            await tx.CommitAsync(ct);
        }

        await context.RespondAsync(new DeleteBoardSuccess(deleted));

        logger.LogInformation("Delete board result for {BoardId}: {Deleted}", request.BoardId, deleted);
    }
}
