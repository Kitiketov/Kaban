using Contracts.Features.Teams.ChangeMemberRole;
using Contracts.Features.Teams.Common;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Teams;

public sealed class ChangeMemberRoleConsumer(
    KabanDatabaseContext db,
    TeamAccess access,
    ILogger<ChangeMemberRoleConsumer> logger) : IConsumer<ChangeMemberRoleRequest>
{
    public async Task Consume(ConsumeContext<ChangeMemberRoleRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0 || request.TeamId <= 0 || request.UserId <= 0 ||
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

        var role = await db.Roles
            .FirstOrDefaultAsync(x => x.TeamId == request.TeamId && x.RoleName == roleName, ct);
        if (role is null)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new TeamFailure("role_not_found"));
            return;
        }

        var membership = await db.UserTeams
            .FirstOrDefaultAsync(x => x.TeamId == request.TeamId && x.UserId == request.UserId, ct);
        if (membership is null)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new TeamFailure("member_not_found"));
            return;
        }

        membership.RoleId = role.Id;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await context.RespondAsync(new MemberRoleChanged(request.TeamId, request.UserId, role.RoleName));
        logger.LogInformation("Changed role of member {UserId} in team {TeamId} to '{Role}'",
            request.UserId, request.TeamId, role.RoleName);
    }
}