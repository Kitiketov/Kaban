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

        string? failureCode = null;
        TeamMemberRemoved? result = null;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await db.Database.AcquireResourceLockAsync(LockResource.Team, request.TeamId, ct);

            failureCode = await access.EnsureTeamMemberAsync(request.TeamId, request.ActorId, ct);
            if (failureCode is null)
            {
                var membership = await db.UserTeams
                    .FirstOrDefaultAsync(x => x.TeamId == request.TeamId && x.UserId == request.UserId, ct);
                if (membership is null)
                    failureCode = "member_not_found";
                else if (await db.UserTeams.CountAsync(x => x.TeamId == request.TeamId, ct) <= 1)
                {
                    failureCode = "last_member";
                }
                else
                {
                    db.Remove(membership);
                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    result = new TeamMemberRemoved(request.TeamId, request.UserId);
                }
            }
        }

        if (failureCode is not null)
        {
            await context.RespondAsync(new TeamFailure(failureCode));
            return;
        }

        var memberRemoved = result ?? throw new InvalidOperationException("Team member result was not created.");
        await context.RespondAsync(memberRemoved);
        logger.LogInformation("Removed member {UserId} from team {TeamId}", request.UserId, request.TeamId);
    }
}
