using Contracts.Features.Projects.Common;
using Contracts.Features.Projects.List;
using Kaban.Worker.Features.Teams;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Projects;

public sealed class ListProjectsConsumer(
    KabanDatabaseContext db,
    TeamAccess access,
    ILogger<ListProjectsConsumer> logger) : IConsumer<ListTeamProjectsRequest>
{
    public async Task Consume(ConsumeContext<ListTeamProjectsRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0 || request.TeamId <= 0)
        {
            await context.RespondAsync(new ProjectFailure("invalid_request"));
            return;
        }

        var teamExists = await db.Teams.AsNoTracking().AnyAsync(x => x.Id == request.TeamId, ct);
        if (!teamExists)
        {
            await context.RespondAsync(new ProjectFailure("team_not_found"));
            return;
        }

        var accessError = await access.EnsureMemberAsync(request.TeamId, request.ActorId, ct);
        if (accessError is not null)
        {
            await context.RespondAsync(new ProjectFailure(accessError));
            return;
        }

        var projects = await db.TeamProjects.AsNoTracking()
            .Where(x => x.TeamId == request.TeamId)
            .Select(x => new ProjectSummary(x.ProjectId, x.TeamId, x.Project!.ProjectName))
            .ToListAsync(ct);

        await context.RespondAsync(new ProjectListResult(projects));
        logger.LogInformation("Returned {ProjectCount} projects for team {TeamId}", projects.Count, request.TeamId);
    }
}