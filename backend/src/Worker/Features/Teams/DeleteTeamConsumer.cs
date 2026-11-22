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

        var projectIds = await db.TeamProjects.AsNoTracking()
            .Where(x => x.TeamId == request.TeamId)
            .Select(x => x.ProjectId)
            .ToListAsync(ct);

        await db.Projects.Where(x => projectIds.Contains(x.Id)).ExecuteDeleteAsync(ct);
        await db.Teams.Where(x => x.Id == request.TeamId).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);

        await context.RespondAsync(new TeamDeleted(request.TeamId));
        logger.LogInformation("Deleted team {TeamId} with {ProjectCount} projects", request.TeamId, projectIds.Count);
    }
}