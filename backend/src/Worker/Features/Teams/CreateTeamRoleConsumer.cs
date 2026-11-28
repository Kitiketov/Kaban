using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.CreateRole;
using Kaban.Worker.Features.Teams.Domain;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
namespace Kaban.Worker.Features.Teams;

public sealed class CreateTeamRoleConsumer(
    KabanDatabaseContext db,
    TeamAccess access,
    ILogger<CreateTeamRoleConsumer> logger) : IConsumer<CreateTeamRoleRequest>
{
    public async Task Consume(ConsumeContext<CreateTeamRoleRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0 || request.TeamId <= 0 ||
            string.IsNullOrWhiteSpace(request.Role) || request.Role.Trim().Length > 64)
        {
            await context.RespondAsync(new TeamFailure("invalid_request"));
            return;
        }

        var roleName = request.Role.Trim();

        string? failureCode = null;
        TeamRoleCreated? result = null;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await db.Database.AcquireResourceLockAsync(LockResource.Team, request.TeamId, ct);

            failureCode = await access.EnsureTeamMemberAsync(request.TeamId, request.ActorId, ct);
            if (failureCode is null &&
                await db.Roles.AnyAsync(x => x.TeamId == request.TeamId && x.RoleName == roleName, ct))
                failureCode = "role_conflict";

            if (failureCode is null)
            {
                var role = new Roles { TeamId = request.TeamId, RoleName = roleName };
                db.Add(role);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                result = new TeamRoleCreated(request.TeamId, role.RoleName);
            }
        }

        if (failureCode is not null)
        {
            await context.RespondAsync(new TeamFailure(failureCode));
            return;
        }

        var roleCreated = result ?? throw new InvalidOperationException("Team role result was not created.");
        await context.RespondAsync(roleCreated);
        logger.LogInformation("Created role '{Role}' for team {TeamId}", roleCreated.Role, request.TeamId);
    }
}