using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.CreateRole;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using RoleEntity = Kaban.Worker.Infrastructure.Database.Domain.Roles;

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

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({request.TeamId})", ct);

        var teamExists = await db.Teams.AnyAsync(x => x.Id == request.TeamId, ct);
        if (!teamExists)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new TeamFailure("team_not_found"));
            return;
        }

        var accessError = await access.EnsureMemberAsync(request.TeamId, request.ActorId, ct);
        if (accessError is not null)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new TeamFailure(accessError));
            return;
        }

        var roleExists = await db.Roles
            .AnyAsync(x => x.TeamId == request.TeamId && x.RoleName == roleName, ct);
        if (roleExists)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new TeamFailure("role_conflict"));
            return;
        }

        var role = new RoleEntity { TeamId = request.TeamId, RoleName = roleName };
        db.Add(role);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await context.RespondAsync(new TeamRoleCreated(request.TeamId, role.RoleName));
        logger.LogInformation("Created role '{Role}' for team {TeamId}", role.RoleName, request.TeamId);
    }
}