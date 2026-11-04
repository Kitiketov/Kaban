using Contracts.Features.Boards;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Boards;

public sealed class RenameBoardConsumer(KabanDatabaseContext db, ILogger<RenameBoardConsumer> logger)
    : IConsumer<RenameBoardRequest>
{
    public async Task Consume(ConsumeContext<RenameBoardRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;

        var board = await db.Boards.FirstOrDefaultAsync(x => x.Id == request.BoardId, ct);

        if (board is null)
            throw new InvalidOperationException("Board not found.");

        board.Name = request.Name;
        await db.SaveChangesAsync(ct);
        await context.RespondAsync(new BoardRenamed(board.Id, board.ProjectId, board.Name));

        logger.LogInformation("Renamed board {BoardId} for project {ProjectId}", board.Id, board.ProjectId);
    }
}