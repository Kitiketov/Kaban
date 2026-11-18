using Contracts.Features.Boards;
using Kaban.Worker.Infrastructure.Database;
using Kaban.Worker.Infrastructure.Database.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using BoardEntity = Kaban.Worker.Infrastructure.Database.Domain.Boards;

namespace Kaban.Worker.Features.Boards;

public sealed class CreateBoardConsumer(KabanDatabaseContext db, ILogger<CreateBoardConsumer> logger)
    : IConsumer<CreateBoardRequest>
{
    public async Task Consume(ConsumeContext<CreateBoardRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (!await db.Projects.AnyAsync(x => x.Id == request.ProjectId, ct))
        {
            await db.Projects.AddAsync(new Projects {Id = request.ProjectId, ProjectName = "WW"}, ct);
            //throw new InvalidOperationException("Project not found.");
        }

        var board = new BoardEntity { ProjectId = request.ProjectId, Name = request.Name };
        db.Add(board);
        await db.SaveChangesAsync(ct);
        await context.RespondAsync(new BoardCreated(board.Id, board.ProjectId, board.Name));

        logger.LogInformation("Created board {BoardId} for project {ProjectId}", board.Id, board.ProjectId);
    }
}