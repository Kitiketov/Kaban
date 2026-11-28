using Contracts.Features.Projects.Common;
using Contracts.Features.Projects.Get;
using Kaban.Worker.Features.Teams;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Projects;

public sealed class GetProjectConsumer(
    KabanDatabaseContext db,
    TeamAccess access,
    ILogger<GetProjectConsumer> logger) : IConsumer<GetProjectRequest>
{
    public async Task Consume(ConsumeContext<GetProjectRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0 || request.ProjectId <= 0)
        {
            await context.RespondAsync(new ProjectFailure("invalid_request"));
            return;
        }

        var project = await db.TeamProjects.AsNoTracking()
            .Where(x => x.ProjectId == request.ProjectId)
            .Select(x => new { x.ProjectId, x.TeamId, x.Project!.ProjectName })
            .FirstOrDefaultAsync(ct);
        if (project is null)
        {
            await context.RespondAsync(new ProjectFailure("project_not_found"));
            return;
        }

        var accessError = await access.EnsureMemberAsync(project.TeamId, request.ActorId, ct);
        if (accessError is not null)
        {
            await context.RespondAsync(new ProjectFailure(accessError));
            return;
        }

        await context.RespondAsync(new ProjectDetails(project.ProjectId, project.TeamId, project.ProjectName));
        logger.LogInformation("Returned project {ProjectId} of team {TeamId}", project.ProjectId, project.TeamId);
    }
}