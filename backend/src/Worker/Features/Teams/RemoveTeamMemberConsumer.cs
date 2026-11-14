using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.RemoveMember;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Teams;

public sealed class RemoveTeamMemberConsumer(
    KabanDatabaseContext db,
    TeamAccess access,
    ILogger<RemoveTeamMemberConsumer> logger) : IConsumer<RemoveTeamMemberRequest>
{
    public async Task Consume(ConsumeContext<RemoveTeamMemberRequest> context)
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

        var membership = await db.UserTeams
            .FirstOrDefaultAsync(x => x.TeamId == request.TeamId && x.UserId == request.UserId, ct);
        if (membership is null)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new TeamFailure("member_not_found"));
            return;
        }

        var memberCount = await db.UserTeams.CountAsync(x => x.TeamId == request.TeamId, ct);
        if (memberCount <= 1)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new TeamFailure("last_member"));
            return;
        }

        db.Remove(membership);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await context.RespondAsync(new TeamMemberRemoved(request.TeamId, request.UserId));
        logger.LogInformation("Removed member {UserId} from team {TeamId}", request.UserId, request.TeamId);
    }
}