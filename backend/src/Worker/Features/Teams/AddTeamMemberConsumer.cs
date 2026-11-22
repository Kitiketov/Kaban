using Contracts.Features.Teams.AddMember;
using Contracts.Features.Teams.Common;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserTeamEntity = Kaban.Worker.Infrastructure.Database.Domain.UserTeams;

namespace Kaban.Worker.Features.Teams;

public sealed class AddTeamMemberConsumer(
    KabanDatabaseContext db,
    TeamAccess access,
    ILogger<AddTeamMemberConsumer> logger) : IConsumer<AddTeamMemberRequest>
{
    public async Task Consume(ConsumeContext<AddTeamMemberRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0 || request.TeamId <= 0 || request.UserId <= 0)
        {
            await context.RespondAsync(new TeamFailure("invalid_request"));
            return;
        }

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

        var userExists = await db.Users.AnyAsync(x => x.Id == request.UserId, ct);
        if (!userExists)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new TeamFailure("user_not_found"));
            return;
        }

        var alreadyMember = await db.UserTeams
            .AnyAsync(x => x.TeamId == request.TeamId && x.UserId == request.UserId, ct);
        if (alreadyMember)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new TeamFailure("member_conflict"));
            return;
        }

        var memberRole = await db.Roles
            .SingleAsync(x => x.TeamId == request.TeamId && x.RoleName == TeamRoles.Member, ct);
        db.Add(new UserTeamEntity { UserId = request.UserId, TeamId = request.TeamId, RoleId = memberRole.Id });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await context.RespondAsync(new TeamMemberAdded(request.TeamId, request.UserId, memberRole.RoleName));
        logger.LogInformation("Added member {UserId} to team {TeamId} with role '{Role}'",
            request.UserId, request.TeamId, memberRole.RoleName);
    }
}