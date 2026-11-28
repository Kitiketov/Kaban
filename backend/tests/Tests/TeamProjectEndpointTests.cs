using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
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
using Kaban.Api.Features.Auth.Infrastructure;
using Kaban.Api.Features.Projects.Application.DTO;
using Kaban.Api.Features.Teams.Application.DTO;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Kaban.Tests;

public sealed class TeamProjectEndpointTests : IDisposable
{
    private const int ActorId = 7;
    private readonly TeamApiFactory factory;
    private readonly HttpClient client;

    public TeamProjectEndpointTests()
    {
        factory = new TeamApiFactory();
        client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new(
            "Bearer",
            factory.Services.GetRequiredService<TokenService>().Create(ActorId).AccessToken);
    }

    [Fact]
    public async Task TeamAndProjectEndpoints_MapWorkerResponses()
    {
        factory.CreateTeam
            .Setup(x => x.GetResponse<TeamCreated, TeamFailure>(
                new CreateTeamRequest(ActorId, "New Team"), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<TeamCreated, TeamFailure>(new TeamCreated(55, "New Team")));
        factory.ListTeams
            .Setup(x => x.GetResponse<TeamListResult, TeamFailure>(
                new ListTeamsRequest(ActorId), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<TeamListResult, TeamFailure>(
                new TeamListResult([new TeamSummary(55, "New Team", "owner")])));
        factory.GetTeam
            .Setup(x => x.GetResponse<TeamDetails, TeamFailure>(
                new GetTeamRequest(ActorId, 55), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<TeamDetails, TeamFailure>(new TeamDetails(
                55, "New Team", [new TeamMemberDetails(ActorId, "Owner", "owner@example.com", "owner")])));
        factory.AddTeamMember
            .Setup(x => x.GetResponse<TeamMemberAdded, TeamFailure>(
                new AddTeamMemberRequest(ActorId, 55, 8), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<TeamMemberAdded, TeamFailure>(new TeamMemberAdded(55, 8, "member")));
        factory.ChangeMemberRole
            .Setup(x => x.GetResponse<MemberRoleChanged, TeamFailure>(
                new ChangeMemberRoleRequest(ActorId, 55, 8, "admin"),
                It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<MemberRoleChanged, TeamFailure>(new MemberRoleChanged(55, 8, "admin")));
        factory.CreateTeamRole
            .Setup(x => x.GetResponse<TeamRoleCreated, TeamFailure>(
                new CreateTeamRoleRequest(ActorId, 55, "qa"),
                It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<TeamRoleCreated, TeamFailure>(new TeamRoleCreated(55, "qa")));
        factory.RemoveTeamMember
            .Setup(x => x.GetResponse<TeamMemberRemoved, TeamFailure>(
                new RemoveTeamMemberRequest(ActorId, 55, 8),
                It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<TeamMemberRemoved, TeamFailure>(new TeamMemberRemoved(55, 8)));
        factory.DeleteTeam
            .Setup(x => x.GetResponse<TeamDeleted, TeamFailure>(
                new DeleteTeamRequest(ActorId, 55), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<TeamDeleted, TeamFailure>(new TeamDeleted(55)));
        factory.CreateProject
            .Setup(x => x.GetResponse<ProjectCreated, ProjectFailure>(
                new CreateProjectRequest(ActorId, 55, "New Project"),
                It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<ProjectCreated, ProjectFailure>(new ProjectCreated(66, 55, "New Project")));
        factory.GetProject
            .Setup(x => x.GetResponse<ProjectDetails, ProjectFailure>(
                new GetProjectRequest(ActorId, 66), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<ProjectDetails, ProjectFailure>(new ProjectDetails(66, 55, "New Project")));
        factory.ListProjects
            .Setup(x => x.GetResponse<ProjectListResult, ProjectFailure>(
                new ListTeamProjectsRequest(ActorId, 55),
                It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<ProjectListResult, ProjectFailure>(
                new ProjectListResult([new ProjectSummary(66, 55, "New Project")])));
        factory.DeleteProject
            .Setup(x => x.GetResponse<ProjectDeleted, ProjectFailure>(
                new DeleteProjectRequest(ActorId, 66), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Success<ProjectDeleted, ProjectFailure>(new ProjectDeleted(66)));

        var created = await client.PostAsJsonAsync("/api/teams", new { name = " New Team " });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var team = (await created.Content.ReadFromJsonAsync<TeamDto>())!;
        Assert.Equal(55, team.Id);
        Assert.Equal("New Team", team.Name);

        var teams = await client.GetFromJsonAsync<TeamListDto>("/api/teams");
        Assert.Equal("owner", Assert.Single(teams!.Teams).Role);

        var details = await client.GetFromJsonAsync<TeamDetailsDto>("/api/teams/55");
        Assert.Equal("owner", Assert.Single(details!.Members).Role);

        var added = await client.PostAsJsonAsync("/api/teams/55/members", new { userId = 8 });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        Assert.Equal("member", (await added.Content.ReadFromJsonAsync<TeamMembershipDto>())!.Role);

        var changed = await client.PatchAsJsonAsync(
            "/api/teams/55/members/8/roles", new { role = " admin " });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal("admin", (await changed.Content.ReadFromJsonAsync<TeamMembershipDto>())!.Role);

        var role = await client.PostAsJsonAsync("/api/teams/55/roles", new { role = " qa " });
        Assert.Equal(HttpStatusCode.Created, role.StatusCode);
        Assert.Equal("qa", (await role.Content.ReadFromJsonAsync<TeamRoleDto>())!.Role);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync("/api/teams/55/members/8")).StatusCode);

        var project = await client.PostAsJsonAsync("/api/teams/55/projects", new { name = " New Project " });
        Assert.Equal(HttpStatusCode.Created, project.StatusCode);
        var projectDto = (await project.Content.ReadFromJsonAsync<ProjectDto>())!;
        Assert.Equal((66, 55, "New Project"), (projectDto.Id, projectDto.TeamId, projectDto.Name));

        Assert.Equal("New Project",
            (await client.GetFromJsonAsync<ProjectDto>("/api/projects/66"))!.Name);
        var projects = await client.GetFromJsonAsync<ProjectListDto>("/api/teams/55/projects");
        Assert.Equal(66, Assert.Single(projects!.Projects).Id);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/projects/66")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/teams/55")).StatusCode);
    }

    [Fact]
    public async Task InvalidInput_IsRejectedBeforeMessaging()
    {
        var badEmpty = await client.PostAsJsonAsync("/api/teams", new { name = "" });
        var badLong = await client.PostAsJsonAsync("/api/teams", new { name = new string('x', 65) });
        var badTeamId = await client.PostAsJsonAsync("/api/teams/0/members", new { userId = 8 });
        var badRole = await client.PatchAsJsonAsync("/api/teams/55/members/8/roles", new { role = "" });
        var badProject = await client.PostAsJsonAsync("/api/teams/55/projects", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, badEmpty.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badLong.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badTeamId.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badRole.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badProject.StatusCode);
        factory.VerifyNoRequests();
    }

    [Fact]
    public async Task Endpoints_RequireAuthentication()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/teams")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/teams", new { name = "Nope" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/projects/66")).StatusCode);
        factory.VerifyNoRequests();
    }

    [Fact]
    public async Task TeamAndProjectFailures_AreMappedToDistinctHttpStatuses()
    {
        factory.GetTeam
            .Setup(x => x.GetResponse<TeamDetails, TeamFailure>(
                new GetTeamRequest(ActorId, 88), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Failure<TeamDetails, TeamFailure>(new TeamFailure("team_not_found")));
        factory.GetTeam
            .Setup(x => x.GetResponse<TeamDetails, TeamFailure>(
                new GetTeamRequest(ActorId, 89), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Failure<TeamDetails, TeamFailure>(new TeamFailure("not_a_member")));
        factory.GetProject
            .Setup(x => x.GetResponse<ProjectDetails, ProjectFailure>(
                new GetProjectRequest(ActorId, 88), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Failure<ProjectDetails, ProjectFailure>(new ProjectFailure("project_not_found")));
        factory.GetProject
            .Setup(x => x.GetResponse<ProjectDetails, ProjectFailure>(
                new GetProjectRequest(ActorId, 89), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Failure<ProjectDetails, ProjectFailure>(new ProjectFailure("not_a_member")));
        factory.CreateProject
            .Setup(x => x.GetResponse<ProjectCreated, ProjectFailure>(
                new CreateProjectRequest(ActorId, 55, "New Project"),
                It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Failure<ProjectCreated, ProjectFailure>(new ProjectFailure("team_not_found")));
        factory.CreateTeamRole
            .Setup(x => x.GetResponse<TeamRoleCreated, TeamFailure>(
                new CreateTeamRoleRequest(ActorId, 55, "qa"),
                It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(Failure<TeamRoleCreated, TeamFailure>(new TeamFailure("role_conflict")));

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/teams/88")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/teams/89")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/projects/88")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/projects/89")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsJsonAsync("/api/teams/55/projects", new { name = " New Project " })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/api/teams/55/roles", new { role = " qa " })).StatusCode);
    }

    [Fact]
    public async Task WorkerFaultAndTimeout_AreNotExposedAsSuccessfulResponses()
    {
        factory.GetTeam
            .Setup(x => x.GetResponse<TeamDetails, TeamFailure>(
                new GetTeamRequest(ActorId, 99), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ThrowsAsync(new RequestFaultException(
                "private-database-detail",
                Mock.Of<Fault>(x => x.Exceptions == Array.Empty<ExceptionInfo>())));
        factory.GetTeam
            .Setup(x => x.GetResponse<TeamDetails, TeamFailure>(
                new GetTeamRequest(ActorId, 100), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ThrowsAsync(new RequestTimeoutException(Guid.NewGuid().ToString()));

        var fault = await client.GetAsync("/api/teams/99");
        Assert.Equal(HttpStatusCode.BadGateway, fault.StatusCode);
        Assert.DoesNotContain("private-database-detail", await fault.Content.ReadAsStringAsync());

        var timeout = await client.GetAsync("/api/teams/100");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, timeout.StatusCode);
        Assert.DoesNotContain("private-database-detail", await timeout.Content.ReadAsStringAsync());
    }

    private static Response<TSuccess, TFailure> Success<TSuccess, TFailure>(TSuccess message)
        where TSuccess : class
        where TFailure : class =>
        new(
            Task.FromResult(Mock.Of<Response<TSuccess>>(x => x.Message == message)),
            new TaskCompletionSource<Response<TFailure>>().Task);

    private static Response<TSuccess, TFailure> Failure<TSuccess, TFailure>(TFailure message)
        where TSuccess : class
        where TFailure : class =>
        new(
            new TaskCompletionSource<Response<TSuccess>>().Task,
            Task.FromResult(Mock.Of<Response<TFailure>>(x => x.Message == message)));

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }
}

public sealed class TeamApiFactory : WebApplicationFactory<Kaban.Api.Features.Teams.Presentation.TeamsController>
{
    public Mock<IRequestClient<CreateTeamRequest>> CreateTeam { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<ListTeamsRequest>> ListTeams { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<GetTeamRequest>> GetTeam { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<AddTeamMemberRequest>> AddTeamMember { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<RemoveTeamMemberRequest>> RemoveTeamMember { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<CreateTeamRoleRequest>> CreateTeamRole { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<ChangeMemberRoleRequest>> ChangeMemberRole { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<DeleteTeamRequest>> DeleteTeam { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<CreateProjectRequest>> CreateProject { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<GetProjectRequest>> GetProject { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<ListTeamProjectsRequest>> ListProjects { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<DeleteProjectRequest>> DeleteProject { get; } = new(MockBehavior.Strict);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Auth:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Messaging:RequestTimeoutSeconds", "3");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            ReplaceRequestClient(services, CreateTeam.Object);
            ReplaceRequestClient(services, ListTeams.Object);
            ReplaceRequestClient(services, GetTeam.Object);
            ReplaceRequestClient(services, AddTeamMember.Object);
            ReplaceRequestClient(services, RemoveTeamMember.Object);
            ReplaceRequestClient(services, CreateTeamRole.Object);
            ReplaceRequestClient(services, ChangeMemberRole.Object);
            ReplaceRequestClient(services, DeleteTeam.Object);
            ReplaceRequestClient(services, CreateProject.Object);
            ReplaceRequestClient(services, GetProject.Object);
            ReplaceRequestClient(services, ListProjects.Object);
            ReplaceRequestClient(services, DeleteProject.Object);
        });
    }

    public void VerifyNoRequests()
    {
        CreateTeam.VerifyNoOtherCalls();
        ListTeams.VerifyNoOtherCalls();
        GetTeam.VerifyNoOtherCalls();
        AddTeamMember.VerifyNoOtherCalls();
        RemoveTeamMember.VerifyNoOtherCalls();
        CreateTeamRole.VerifyNoOtherCalls();
        ChangeMemberRole.VerifyNoOtherCalls();
        DeleteTeam.VerifyNoOtherCalls();
        CreateProject.VerifyNoOtherCalls();
        GetProject.VerifyNoOtherCalls();
        ListProjects.VerifyNoOtherCalls();
        DeleteProject.VerifyNoOtherCalls();
    }

    private static void ReplaceRequestClient<T>(IServiceCollection services, IRequestClient<T> client)
        where T : class
    {
        services.RemoveAll<IRequestClient<T>>();
        services.AddSingleton(client);
    }
}
