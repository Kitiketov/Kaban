using Contracts.Features.Projects.Common;
using Contracts.Features.Projects.Delete;
using Kaban.Worker.Features.Teams;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Projects;

public sealed class DeleteProjectConsumer(
    KabanDatabaseContext db,
    TeamAccess access,
    ILogger<DeleteProjectConsumer> logger) : IConsumer<DeleteProjectRequest>
{
    public async Task Consume(ConsumeContext<DeleteProjectRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0 || request.ProjectId <= 0)
        {
            await context.RespondAsync(new ProjectFailure("invalid_request"));
            return;
        }

        var teamId = await db.TeamProjects.AsNoTracking()
            .Where(x => x.ProjectId == request.ProjectId)
            .Select(x => (int?)x.TeamId)
            .FirstOrDefaultAsync(ct);
        if (teamId is null)
        {
            await context.RespondAsync(new ProjectFailure("project_not_found"));
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({teamId.Value})", ct);

        var accessError = await access.EnsureMemberAsync(teamId.Value, request.ActorId, ct);
        if (accessError is not null)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new ProjectFailure(accessError));
            return;
        }

        await db.Projects.Where(x => x.Id == request.ProjectId).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);

        await context.RespondAsync(new ProjectDeleted(request.ProjectId));
        logger.LogInformation("Deleted project {ProjectId} of team {TeamId}", request.ProjectId, teamId.Value);
    }
}