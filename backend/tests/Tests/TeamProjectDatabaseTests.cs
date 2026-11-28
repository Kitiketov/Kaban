using Contracts.Features.Projects.Common;
using Contracts.Features.Projects.Create;
using Contracts.Features.Projects.Delete;
using Contracts.Features.Projects.Get;
using Contracts.Features.Projects.List;
using Contracts.Features.Teams.AddMember;
using Contracts.Features.Teams.ChangeMemberRole;
using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.Create;
using Contracts.Features.Teams.CreateRole;
using Contracts.Features.Teams.Delete;
using Contracts.Features.Teams.Get;
using Contracts.Features.Teams.List;
using Contracts.Features.Teams.RemoveMember;
using Kaban.Worker.Features.Projects;
using Kaban.Worker.Features.Teams;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;
using UserEntity = Kaban.Worker.Features.Users.Domain.Users;

namespace Kaban.Tests;

public sealed class TeamProjectDatabaseTests
{
    [PostgresFact]
    public async Task TeamAndProjectConsumers_PersistChangesAndEnforceMembership()
    {
        var database = "kaban_team_project_" + Guid.NewGuid().ToString("N");
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("KABAN_TEST_POSTGRES"));
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE {database}", admin).ExecuteNonQueryAsync();
        connection.Database = database;
        var options = new DbContextOptionsBuilder<KabanDatabaseContext>()
            .UseNpgsql(connection.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        try
        {
            await using (var db = new KabanDatabaseContext(options))
            {
                await db.Database.MigrateAsync();
                db.Users.AddRange(
                    new UserEntity { Id = 7, Name = "Owner", Email = "owner@example.com", HashedPassword = "hash" },
                    new UserEntity { Id = 8, Name = "Member", Email = "member@example.com", HashedPassword = "hash" });
                await db.SaveChangesAsync();
            }

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<KabanDatabaseContext>(builder => builder
                .UseNpgsql(connection.ConnectionString)
                .UseSnakeCaseNamingConvention());
            services.AddScoped<TeamAccess>();
            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<CreateTeamConsumer>();
                x.AddConsumer<ListTeamsConsumer>();
                x.AddConsumer<GetTeamConsumer>();
                x.AddConsumer<AddTeamMemberConsumer>();
                x.AddConsumer<RemoveTeamMemberConsumer>();
                x.AddConsumer<CreateTeamRoleConsumer>();
                x.AddConsumer<ChangeMemberRoleConsumer>();
                x.AddConsumer<DeleteTeamConsumer>();
                x.AddConsumer<CreateProjectConsumer>();
                x.AddConsumer<GetProjectConsumer>();
                x.AddConsumer<ListProjectsConsumer>();
                x.AddConsumer<DeleteProjectConsumer>();

                x.AddRequestClient<CreateTeamRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<ListTeamsRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<GetTeamRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<AddTeamMemberRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<RemoveTeamMemberRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<CreateTeamRoleRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<ChangeMemberRoleRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<DeleteTeamRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<CreateProjectRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<GetProjectRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<ListTeamProjectsRequest>(RequestTimeout.After(s: 10));
                x.AddRequestClient<DeleteProjectRequest>(RequestTimeout.After(s: 10));
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            });

            await using var provider = services.BuildServiceProvider(true);
            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();
            try
            {
                await using var scope = provider.CreateAsyncScope();
                var teamClient = scope.ServiceProvider.GetRequiredService<IRequestClient<CreateTeamRequest>>();
                var createTeam = await teamClient.GetResponse<TeamCreated, TeamFailure>(
                    new CreateTeamRequest(7, " Product Team "));
                Assert.True(createTeam.Is(out Response<TeamCreated>? createdTeam));
                var teamId = createdTeam.Message.Id;
                Assert.Equal("Product Team", createdTeam.Message.Name);

                await using (var db = new KabanDatabaseContext(options))
                {
                    Assert.Equal("Product Team", await db.Teams.Where(x => x.Id == teamId)
                        .Select(x => x.TeamName).SingleAsync());
                    Assert.Equal(new[] { "member", "owner" }, await db.Roles.Where(x => x.TeamId == teamId)
                        .OrderBy(x => x.RoleName).Select(x => x.RoleName).ToArrayAsync());
                    Assert.Equal("owner", await db.UserTeams.Where(x => x.TeamId == teamId && x.UserId == 7)
                        .Select(x => x.Role!.RoleName).SingleAsync());
                }

                var listTeams = await scope.ServiceProvider.GetRequiredService<IRequestClient<ListTeamsRequest>>()
                    .GetResponse<TeamListResult, TeamFailure>(new ListTeamsRequest(7));
                Assert.True(listTeams.Is(out Response<TeamListResult>? teamList));
                Assert.Contains(teamList.Message.Teams, x => x.Id == teamId && x.Role == "owner");

                var getTeam = await scope.ServiceProvider.GetRequiredService<IRequestClient<GetTeamRequest>>()
                    .GetResponse<TeamDetails, TeamFailure>(new GetTeamRequest(7, teamId));
                Assert.True(getTeam.Is(out Response<TeamDetails>? teamDetails));
                Assert.Contains(teamDetails.Message.Members, x => x.UserId == 7 && x.Role == "owner");

                var missingTeam = await scope.ServiceProvider.GetRequiredService<IRequestClient<GetTeamRequest>>()
                    .GetResponse<TeamDetails, TeamFailure>(new GetTeamRequest(7, teamId + 100));
                Assert.True(missingTeam.Is(out Response<TeamFailure>? missingTeamFailure));
                Assert.Equal("team_not_found", missingTeamFailure.Message.Code);

                var addMember = await scope.ServiceProvider.GetRequiredService<IRequestClient<AddTeamMemberRequest>>()
                    .GetResponse<TeamMemberAdded, TeamFailure>(new AddTeamMemberRequest(7, teamId, 8));
                Assert.True(addMember.Is(out Response<TeamMemberAdded>? memberAdded));
                Assert.Equal("member", memberAdded.Message.Role);

                var missingUser = await scope.ServiceProvider.GetRequiredService<IRequestClient<AddTeamMemberRequest>>()
                    .GetResponse<TeamMemberAdded, TeamFailure>(new AddTeamMemberRequest(7, teamId, 9));
                Assert.True(missingUser.Is(out Response<TeamFailure>? missingUserFailure));
                Assert.Equal("user_not_found", missingUserFailure.Message.Code);

                var duplicateMember = await scope.ServiceProvider.GetRequiredService<IRequestClient<AddTeamMemberRequest>>()
                    .GetResponse<TeamMemberAdded, TeamFailure>(new AddTeamMemberRequest(7, teamId, 8));
                Assert.True(duplicateMember.Is(out Response<TeamFailure>? memberFailure));
                Assert.Equal("member_conflict", memberFailure.Message.Code);

                var createRoleClient = scope.ServiceProvider.GetRequiredService<IRequestClient<CreateTeamRoleRequest>>();
                var createRole = await createRoleClient.GetResponse<TeamRoleCreated, TeamFailure>(
                    new CreateTeamRoleRequest(7, teamId, "qa"));
                Assert.True(createRole.Is(out Response<TeamRoleCreated>? roleCreated));
                Assert.Equal("qa", roleCreated.Message.Role);

                var missingRole = await scope.ServiceProvider.GetRequiredService<IRequestClient<ChangeMemberRoleRequest>>()
                    .GetResponse<MemberRoleChanged, TeamFailure>(
                        new ChangeMemberRoleRequest(7, teamId, 8, "missing"));
                Assert.True(missingRole.Is(out Response<TeamFailure>? missingRoleFailure));
                Assert.Equal("role_not_found", missingRoleFailure.Message.Code);

                var duplicateRole = await createRoleClient.GetResponse<TeamRoleCreated, TeamFailure>(
                    new CreateTeamRoleRequest(7, teamId, "qa"));
                Assert.True(duplicateRole.Is(out Response<TeamFailure>? roleFailure));
                Assert.Equal("role_conflict", roleFailure.Message.Code);

                var missingMember = await scope.ServiceProvider.GetRequiredService<IRequestClient<ChangeMemberRoleRequest>>()
                    .GetResponse<MemberRoleChanged, TeamFailure>(
                        new ChangeMemberRoleRequest(7, teamId, 9, "qa"));
                Assert.True(missingMember.Is(out Response<TeamFailure>? missingMemberFailure));
                Assert.Equal("member_not_found", missingMemberFailure.Message.Code);

                var changeRole = await scope.ServiceProvider.GetRequiredService<IRequestClient<ChangeMemberRoleRequest>>()
                    .GetResponse<MemberRoleChanged, TeamFailure>(
                        new ChangeMemberRoleRequest(7, teamId, 8, "qa"));
                Assert.True(changeRole.Is(out Response<MemberRoleChanged>? roleChanged));
                Assert.Equal("qa", roleChanged.Message.Role);

                var createProjectClient = scope.ServiceProvider.GetRequiredService<IRequestClient<CreateProjectRequest>>();
                var forbiddenProject = await createProjectClient.GetResponse<ProjectCreated, ProjectFailure>(
                    new CreateProjectRequest(9, teamId, "Forbidden"));
                Assert.True(forbiddenProject.Is(out Response<ProjectFailure>? projectFailure));
                Assert.Equal("not_a_member", projectFailure.Message.Code);

                var createProject = await createProjectClient.GetResponse<ProjectCreated, ProjectFailure>(
                    new CreateProjectRequest(7, teamId, "Roadmap"));
                Assert.True(createProject.Is(out Response<ProjectCreated>? projectCreated));
                var projectId = projectCreated.Message.Id;
                Assert.Equal("Roadmap", projectCreated.Message.Name);

                var getProjectClient = scope.ServiceProvider.GetRequiredService<IRequestClient<GetProjectRequest>>();
                var getProject = await getProjectClient.GetResponse<ProjectDetails, ProjectFailure>(
                    new GetProjectRequest(7, projectId));
                Assert.True(getProject.Is(out Response<ProjectDetails>? projectDetails));
                Assert.Equal((teamId, "Roadmap"), (projectDetails.Message.TeamId, projectDetails.Message.Name));

                var forbiddenProjectRead = await getProjectClient.GetResponse<ProjectDetails, ProjectFailure>(
                    new GetProjectRequest(9, projectId));
                Assert.True(forbiddenProjectRead.Is(out Response<ProjectFailure>? forbiddenProjectReadFailure));
                Assert.Equal("not_a_member", forbiddenProjectReadFailure.Message.Code);

                var listProjects = await scope.ServiceProvider.GetRequiredService<IRequestClient<ListTeamProjectsRequest>>()
                    .GetResponse<ProjectListResult, ProjectFailure>(new ListTeamProjectsRequest(7, teamId));
                Assert.True(listProjects.Is(out Response<ProjectListResult>? projectList));
                Assert.Contains(projectList.Message.Projects, x => x.Id == projectId && x.TeamId == teamId);

                await using (var db = new KabanDatabaseContext(options))
                {
                    Assert.True(await db.Projects.AnyAsync(x => x.Id == projectId && x.ProjectName == "Roadmap"));
                    Assert.True(await db.TeamProjects.AnyAsync(x => x.TeamId == teamId && x.ProjectId == projectId));
                    Assert.Equal("qa", await db.UserTeams.Where(x => x.TeamId == teamId && x.UserId == 8)
                        .Select(x => x.Role!.RoleName).SingleAsync());
                }

                var forbiddenProjectDelete = await scope.ServiceProvider.GetRequiredService<IRequestClient<DeleteProjectRequest>>()
                    .GetResponse<ProjectDeleted, ProjectFailure>(new DeleteProjectRequest(9, projectId));
                Assert.True(forbiddenProjectDelete.Is(out Response<ProjectFailure>? forbiddenProjectDeleteFailure));
                Assert.Equal("not_a_member", forbiddenProjectDeleteFailure.Message.Code);

                var removeMember = await scope.ServiceProvider.GetRequiredService<IRequestClient<RemoveTeamMemberRequest>>()
                    .GetResponse<TeamMemberRemoved, TeamFailure>(new RemoveTeamMemberRequest(7, teamId, 8));
                Assert.True(removeMember.Is(out Response<TeamMemberRemoved>? memberRemoved));
                Assert.Equal(8, memberRemoved.Message.UserId);

                var removeLastMember = await scope.ServiceProvider.GetRequiredService<IRequestClient<RemoveTeamMemberRequest>>()
                    .GetResponse<TeamMemberRemoved, TeamFailure>(new RemoveTeamMemberRequest(7, teamId, 7));
                Assert.True(removeLastMember.Is(out Response<TeamFailure>? lastMemberFailure));
                Assert.Equal("last_member", lastMemberFailure.Message.Code);

                var deleteProject = await scope.ServiceProvider.GetRequiredService<IRequestClient<DeleteProjectRequest>>()
                    .GetResponse<ProjectDeleted, ProjectFailure>(new DeleteProjectRequest(7, projectId));
                Assert.True(deleteProject.Is(out Response<ProjectDeleted>? projectDeleted));
                Assert.Equal(projectId, projectDeleted.Message.ProjectId);

                var missingProject = await getProjectClient.GetResponse<ProjectDetails, ProjectFailure>(
                    new GetProjectRequest(7, projectId));
                Assert.True(missingProject.Is(out Response<ProjectFailure>? missingProjectFailure));
                Assert.Equal("project_not_found", missingProjectFailure.Message.Code);

                var cascadeProject = await createProjectClient.GetResponse<ProjectCreated, ProjectFailure>(
                    new CreateProjectRequest(7, teamId, "Cascade"));
                Assert.True(cascadeProject.Is(out Response<ProjectCreated>? cascadeProjectCreated));

                var deleteTeam = await scope.ServiceProvider.GetRequiredService<IRequestClient<DeleteTeamRequest>>()
                    .GetResponse<TeamDeleted, TeamFailure>(new DeleteTeamRequest(7, teamId));
                Assert.True(deleteTeam.Is(out Response<TeamDeleted>? teamDeleted));
                Assert.Equal(teamId, teamDeleted.Message.TeamId);

                await using var verification = new KabanDatabaseContext(options);
                Assert.False(await verification.Teams.AnyAsync(x => x.Id == teamId));
                Assert.False(await verification.Roles.AnyAsync(x => x.TeamId == teamId));
                Assert.False(await verification.UserTeams.AnyAsync(x => x.TeamId == teamId));
                Assert.False(await verification.TeamProjects.AnyAsync(x => x.TeamId == teamId));
                Assert.False(await verification.Projects.AnyAsync(x => x.Id == projectId));
                Assert.False(await verification.Projects.AnyAsync(x => x.Id == cascadeProjectCreated.Message.Id));
            }
            finally
            {
                await harness.Stop();
            }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", admin).ExecuteNonQueryAsync();
        }
    }
}
