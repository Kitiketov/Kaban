using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.Create;
using Kaban.Worker.Infrastructure.Database;
using Kaban.Worker.Infrastructure.Database.Domain;
using MassTransit;
using TeamEntity = Kaban.Worker.Infrastructure.Database.Domain.Teams;

namespace Kaban.Worker.Features.Teams;

public sealed class CreateTeamConsumer(KabanDatabaseContext db, ILogger<CreateTeamConsumer> logger)
    : IConsumer<CreateTeamRequest>
{
    public async Task Consume(ConsumeContext<CreateTeamRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0 || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 64)
        {
            await context.RespondAsync(new TeamFailure("invalid_request"));
            return;
        }

        var team = new TeamEntity { TeamName = request.Name.Trim() };
        var ownerRole = new Roles { RoleName = TeamRoles.Owner, TeamId = team.Id, Team = team };
        var memberRole = new Roles { RoleName = TeamRoles.Member, TeamId = team.Id, Team = team };
        db.AddRange(team, ownerRole, memberRole);
        db.Add(new UserTeams
            { UserId = request.ActorId, TeamId = team.Id, Team = team, RoleId = ownerRole.Id, Role = ownerRole });
        await db.SaveChangesAsync(ct);

        await context.RespondAsync(new TeamCreated(team.Id, team.TeamName));
        logger.LogInformation("Created team {TeamId} with owner {ActorId}", team.Id, request.ActorId);
    }
}