using Contracts.Features.Teams.AddMember;
using Contracts.Features.Teams.Common;
using Kaban.Worker.Features.Teams.Domain;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
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

        string? failureCode = null;
        TeamMemberAdded? result = null;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await db.Database.AcquireResourceLockAsync(LockResource.Team, request.TeamId, ct);

            failureCode = await access.EnsureTeamMemberAsync(request.TeamId, request.ActorId, ct);
            if (failureCode is null)
            {
                var userExists = await db.Users.AnyAsync(x => x.Id == request.UserId, ct);
                if (!userExists)
                    failureCode = "user_not_found";
            }

            if (failureCode is null)
            {
                var alreadyMember = await db.UserTeams
                    .AnyAsync(x => x.TeamId == request.TeamId && x.UserId == request.UserId, ct);
                if (alreadyMember)
                    failureCode = "member_conflict";
            }

            if (failureCode is null)
            {
                var memberRole = await db.Roles
                    .SingleAsync(x => x.TeamId == request.TeamId && x.RoleName == TeamRoles.Member, ct);
                db.Add(new UserTeams
                    { UserId = request.UserId, TeamId = request.TeamId, RoleId = memberRole.Id });
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                result = new TeamMemberAdded(request.TeamId, request.UserId, memberRole.RoleName);
            }
        }

        if (failureCode is not null)
        {
            await context.RespondAsync(new TeamFailure(failureCode));
            return;
        }

        var memberAdded = result ?? throw new InvalidOperationException("Team member result was not created.");
        await context.RespondAsync(memberAdded);
        logger.LogInformation("Added member {UserId} to team {TeamId} with role '{Role}'",
            request.UserId, request.TeamId, memberAdded.Role);
    }
}