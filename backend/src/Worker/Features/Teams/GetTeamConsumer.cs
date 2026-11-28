using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.Get;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Teams;

public sealed class GetTeamConsumer(
    KabanDatabaseContext db,
    TeamAccess access,
    ILogger<GetTeamConsumer> logger) : IConsumer<GetTeamRequest>
{
    public async Task Consume(ConsumeContext<GetTeamRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0 || request.TeamId <= 0)
        {
            await context.RespondAsync(new TeamFailure("invalid_request"));
            return;
        }

        var team = await db.Teams.AsNoTracking()
            .Where(x => x.Id == request.TeamId)
            .Select(x => new { x.Id, x.TeamName })
            .FirstOrDefaultAsync(ct);
        if (team is null)
        {
            await context.RespondAsync(new TeamFailure("team_not_found"));
            return;
        }

        var accessError = await access.EnsureMemberAsync(request.TeamId, request.ActorId, ct);
        if (accessError is not null)
        {
            await context.RespondAsync(new TeamFailure(accessError));
            return;
        }

        var members = await db.UserTeams.AsNoTracking()
            .Where(x => x.TeamId == team.Id)
            .Select(x => new TeamMemberDetails(
                x.UserId,
                x.User!.Name,
                x.User!.Email,
                x.Role!.RoleName))
            .ToListAsync(ct);

        await context.RespondAsync(new TeamDetails(team.Id, team.TeamName, members));
        logger.LogInformation("Returned team {TeamId} with {MemberCount} members", team.Id, members.Count);
    }
}