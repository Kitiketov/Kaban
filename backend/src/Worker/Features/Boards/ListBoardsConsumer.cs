using Contracts.Features.Boards;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Boards;

public sealed class ListBoardsConsumer(KabanDatabaseContext db, ILogger<ListBoardsConsumer> logger)
    : IConsumer<ListBoardsRequest>
{
    public async Task Consume(ConsumeContext<ListBoardsRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;

        var rows = await db.Boards.AsNoTracking().Where(x => x.ProjectId == request.ProjectId)
            .OrderBy(x => x.Id).Select(x => new BoardSummary(x.Id, x.ProjectId, x.Name))
            .ToListAsync(ct);
        await context.RespondAsync(new BoardListResult(rows));

        logger.LogInformation("Returned {BoardCount} boards for project {ProjectId}", rows.Count, request.ProjectId);
    }
}