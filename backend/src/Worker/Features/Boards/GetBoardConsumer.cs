using Contracts.Features.Boards;
using Kaban.Worker.Features.Tasks.Application;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Boards;

public sealed class GetBoardConsumer(KabanDatabaseContext db, 
    ILogger<GetBoardConsumer> logger)
    : IConsumer<GetBoardRequest>
{
    public async Task Consume(ConsumeContext<GetBoardRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;

        var board = await db.Boards.AsNoTracking().Where(x => x.Id == request.BoardId)
            .Select(x => new { x.Id, x.ProjectId, x.Name }).FirstOrDefaultAsync(ct);

        if (board is null)
            throw new InvalidOperationException("Board not found.");

        var columns = await db.BoardColumns.AsNoTracking().Where(x => x.BoardId == board.Id)
            .OrderBy(x => x.BoardOrder).Select(x => new BoardColumnDetails(x.Id, x.ColumnName, x.BoardOrder, 
               x.Tasks.Select(t => TaskService.ToTaskSuccess(t)).ToList())).ToListAsync(ct);
        await context.RespondAsync(new BoardDetails(board.Id, board.ProjectId, board.Name, columns));

        logger.LogInformation("Returned board {BoardId} with {ColumnCount} columns", board.Id, columns.Count);
    }
}