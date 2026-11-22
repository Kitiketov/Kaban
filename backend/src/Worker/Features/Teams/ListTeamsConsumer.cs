using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.List;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Teams;

public sealed class ListTeamsConsumer(KabanDatabaseContext db, ILogger<ListTeamsConsumer> logger)
    : IConsumer<ListTeamsRequest>
{
    public async Task Consume(ConsumeContext<ListTeamsRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0)
        {
            await context.RespondAsync(new TeamFailure("invalid_request"));
            return;
        }

        var teams = await db.UserTeams.AsNoTracking()
            .Where(x => x.UserId == request.ActorId)
            .Select(x => new TeamSummary(
                x.TeamId,
                x.Team!.TeamName,
                x.Role!.RoleName))
            .ToListAsync(ct);

        await context.RespondAsync(new TeamListResult(teams));
        logger.LogInformation("Returned {TeamCount} teams for actor {ActorId}", teams.Count, request.ActorId);
    }
}