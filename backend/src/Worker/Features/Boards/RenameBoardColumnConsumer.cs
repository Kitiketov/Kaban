using Contracts.Features.Boards;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Boards;

public sealed class RenameBoardColumnConsumer(KabanDatabaseContext db, ILogger<RenameBoardColumnConsumer> logger)
    : IConsumer<RenameBoardColumnRequest>
{
    public async Task Consume(ConsumeContext<RenameBoardColumnRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;

        var column = await db.BoardColumns.FirstOrDefaultAsync(x => x.Id == request.ColumnId, ct);

        if (column is null)
            throw new InvalidOperationException("Board column not found.");

        column.ColumnName = request.Name;
        await db.SaveChangesAsync(ct);
        await context.RespondAsync(new BoardColumnRenamed(column.Id, column.BoardId, column.ColumnName,
            column.BoardOrder));

        logger.LogInformation("Renamed column {ColumnId} on board {BoardId}; correlation {CorrelationId}", column.Id,
            column.BoardId, context.CorrelationId);
    }
}