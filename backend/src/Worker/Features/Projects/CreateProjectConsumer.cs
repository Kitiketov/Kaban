using Contracts.Features.Projects.Common;
using Contracts.Features.Projects.Create;
using Kaban.Worker.Features.Projects.Domain;
using Kaban.Worker.Features.Teams;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using ProjectEntity = Kaban.Worker.Features.Projects.Domain.Projects;

namespace Kaban.Worker.Features.Projects;

public sealed class CreateProjectConsumer(
    KabanDatabaseContext db,
    TeamAccess access,
    ILogger<CreateProjectConsumer> logger) : IConsumer<CreateProjectRequest>
{
    public async Task Consume(ConsumeContext<CreateProjectRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        if (request.ActorId <= 0 || request.TeamId <= 0 ||
            string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 64)
        {
            await context.RespondAsync(new ProjectFailure("invalid_request"));
            return;
        }

        string? failureCode = null;
        ProjectCreated? result = null;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await db.Database.AcquireResourceLockAsync(LockResource.Team, request.TeamId, ct);

            failureCode = await access.EnsureTeamMemberAsync(request.TeamId, request.ActorId, ct);
            if (failureCode is null)
            {
                var project = new ProjectEntity { ProjectName = request.Name.Trim() };
                db.Add(project);
                db.Add(new TeamProjects { ProjectId = project.Id, Project = project, TeamId = request.TeamId });
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                result = new ProjectCreated(project.Id, request.TeamId, project.ProjectName);
            }
        }

        if (failureCode is not null)
        {
            await context.RespondAsync(new ProjectFailure(failureCode));
            return;
        }

        var projectCreated = result ?? throw new InvalidOperationException("Project result was not created.");
        await context.RespondAsync(projectCreated);
        logger.LogInformation("Created project {ProjectId} for team {TeamId}", projectCreated.Id, request.TeamId);
    }
}
