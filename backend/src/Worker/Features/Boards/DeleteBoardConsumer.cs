using Contracts.Features.Boards;
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

        var board = await db.Boards.FirstOrDefaultAsync(x => x.Id == request.BoardId, ct);
        if (board is null)
        {
            await context.RespondAsync(new BoardDeleted(false));
            logger.LogInformation("Delete board result for {BoardId}: {Deleted}", request.BoardId, false);
            return;
        }

        db.Remove(board);
        await db.SaveChangesAsync(ct);
        await context.RespondAsync(new BoardDeleted(true));

        logger.LogInformation("Delete board result for {BoardId}: {Deleted}", request.BoardId, true);
    }
}