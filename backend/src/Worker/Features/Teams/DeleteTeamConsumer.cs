using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.Delete;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Teams;

public sealed class DeleteTeamConsumer(
    KabanDatabaseContext db,
    TeamAccess access,
    ILogger<DeleteTeamConsumer> logger) : IConsumer<DeleteTeamRequest>
{
    public async Task Consume(ConsumeContext<DeleteTeamRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0 || request.TeamId <= 0)
        {
            await context.RespondAsync(new TeamFailure("invalid_request"));
            return;
        }

        string? failureCode = null;
        var projectCount = 0;
        TeamDeleted? result = null;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await db.Database.AcquireResourceLockAsync(LockResource.Team, request.TeamId, ct);

            failureCode = await access.EnsureTeamMemberAsync(request.TeamId, request.ActorId, ct);
            if (failureCode is null)
            {
                var projectIds = await db.TeamProjects.AsNoTracking()
                    .Where(x => x.TeamId == request.TeamId)
                    .Select(x => x.ProjectId)
                    .ToListAsync(ct);

                await db.Projects.Where(x => projectIds.Contains(x.Id)).ExecuteDeleteAsync(ct);
                await db.Teams.Where(x => x.Id == request.TeamId).ExecuteDeleteAsync(ct);
                await transaction.CommitAsync(ct);
                projectCount = projectIds.Count;
                result = new TeamDeleted(request.TeamId);
            }
        }

        if (failureCode is not null)
        {
            await context.RespondAsync(new TeamFailure(failureCode));
            return;
        }

        var teamDeleted = result ?? throw new InvalidOperationException("Team deletion did not complete.");
        await context.RespondAsync(teamDeleted);
        logger.LogInformation("Deleted team {TeamId} with {ProjectCount} projects", request.TeamId, projectCount);
    }
}
