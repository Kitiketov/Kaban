using Contracts.Features.Projects.Common;
using Contracts.Features.Projects.Create;
using Kaban.Worker.Features.Teams;
using Kaban.Worker.Infrastructure.Database;
using Kaban.Worker.Infrastructure.Database.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ProjectEntity = Kaban.Worker.Infrastructure.Database.Domain.Projects;

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

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({request.TeamId})", ct);

        var teamExists = await db.Teams.AnyAsync(x => x.Id == request.TeamId, ct);
        if (!teamExists)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new ProjectFailure("team_not_found"));
            return;
        }

        var accessError = await access.EnsureMemberAsync(request.TeamId, request.ActorId, ct);
        if (accessError is not null)
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new ProjectFailure(accessError));
            return;
        }

        var project = new ProjectEntity { ProjectName = request.Name.Trim() };
        db.Add(project);
        db.Add(new TeamProjects { ProjectId = project.Id, Project = project, TeamId = request.TeamId });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await context.RespondAsync(new ProjectCreated(project.Id, request.TeamId, project.ProjectName));
        logger.LogInformation("Created project {ProjectId} for team {TeamId}", project.Id, request.TeamId);
    }
}