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
using Kaban.Api.Features.Teams.Application.DTO;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kaban.Tests;

public sealed class TeamProjectEndpointTests : IClassFixture<TeamApiFactory>
{
    private readonly TeamApiFactory factory;
    private readonly HttpClient client;

    public TeamProjectEndpointTests(TeamApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new("Bearer", factory.Services.GetRequiredService<TokenService>().Create(7).AccessToken);
    }

    [Fact]
    public async Task TeamAndProjectLifecycle_CompletesEndToEnd()
    {
        var created = await client.PostAsJsonAsync("/api/teams", new { name = " New Team " });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var team = (await created.Content.ReadFromJsonAsync<TeamDto>())!;
        Assert.Equal(55, team.Id);
        Assert.Equal("New Team", team.Name);

        var list = await client.GetFromJsonAsync<TeamListDto>("/api/teams");
        Assert.Single(list!.Teams);
        Assert.Equal("owner", list.Teams[0].Role);

        var details = await client.GetFromJsonAsync<TeamDetailsDto>("/api/teams/55");
        Assert.Equal(55, details!.Id);
        Assert.Equal("owner", details.Members.Single(m => m.UserId == 7).Role);

        var added = await client.PostAsJsonAsync("/api/teams/55/members", new { userId = 8 });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var membership = (await added.Content.ReadFromJsonAsync<TeamMembershipDto>())!;
        Assert.Equal(55, membership.TeamId);
        Assert.Equal(8, membership.UserId);
        Assert.Equal("member", membership.Role);

        var changed = await client.PatchAsJsonAsync("/api/teams/55/members/8/roles", new { role = "admin" });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal("admin", (await changed.Content.ReadFromJsonAsync<TeamMembershipDto>())!.Role);

        var role = await client.PostAsJsonAsync("/api/teams/55/roles", new { role = "qa" });
        Assert.Equal(HttpStatusCode.Created, role.StatusCode);
        Assert.Equal("qa", (await role.Content.ReadFromJsonAsync<TeamRoleDto>())!.Role);

        var project = await client.PostAsJsonAsync("/api/teams/55/projects", new { name = " New Project " });
        Assert.Equal(HttpStatusCode.Created, project.StatusCode);
        var projectDto = (await project.Content.ReadFromJsonAsync<Kaban.Api.Features.Projects.Application.DTO.ProjectDto>())!;
        Assert.Equal(66, projectDto.Id);
        Assert.Equal(55, projectDto.TeamId);
        Assert.Equal("New Project", projectDto.Name);

        var projectDetails = await client.GetFromJsonAsync<Kaban.Api.Features.Projects.Application.DTO.ProjectDto>("/api/projects/66");
        Assert.Equal(55, projectDetails!.TeamId);

        var projectList = await client.GetFromJsonAsync<Kaban.Api.Features.Projects.Application.DTO.ProjectListDto>("/api/teams/55/projects");
        Assert.Single(projectList!.Projects);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/projects/66")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/teams/55")).StatusCode);
    }

    [Fact]
    public async Task InvalidInput_IsRejectedBeforeMessaging()
    {
        var before = factory.Services.GetRequiredService<Probe>().Calls;
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
        Assert.Equal(before, factory.Services.GetRequiredService<Probe>().Calls);
    }

    [Fact]
    public async Task Endpoints_RequireAuthentication()
    {
        var before = factory.Services.GetRequiredService<Probe>().Calls;
        using var anonymous = factory.CreateClient();
        var teamsGet = await anonymous.GetAsync("/api/teams");
        var teamsPost = await anonymous.PostAsJsonAsync("/api/teams", new { name = "Nope" });
        var projectsGet = await anonymous.GetAsync("/api/projects/66");
        Assert.Equal(HttpStatusCode.Unauthorized, teamsGet.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, teamsPost.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, projectsGet.StatusCode);
        Assert.Equal(before, factory.Services.GetRequiredService<Probe>().Calls);
    }

    [Theory]
    [InlineData(88, HttpStatusCode.NotFound)]
    [InlineData(89, HttpStatusCode.Forbidden)]
    [InlineData(87, HttpStatusCode.NotFound)]
    [InlineData(86, HttpStatusCode.Conflict)]
    public async Task TeamBusinessFailures_AreMappedToDistinctHttpStatuses(int targetId, HttpStatusCode expected)
    {
        var response = targetId is 87 or 86
            ? await client.DeleteAsync($"/api/teams/55/members/{targetId}")
            : await client.GetAsync($"/api/teams/{targetId}");
        Assert.Equal(expected, response.StatusCode);
        Assert.DoesNotContain("private-database-detail", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(85, HttpStatusCode.Conflict)]
    [InlineData(84, HttpStatusCode.NotFound)]
    public async Task RoleBusinessFailures_AreMappedToDistinctHttpStatuses(int teamId, HttpStatusCode expected)
    {
        var response = teamId == 85
            ? await client.PostAsJsonAsync($"/api/teams/{teamId}/roles", new { role = "dup" })
            : await client.PatchAsJsonAsync($"/api/teams/{teamId}/members/8/roles", new { role = "ghost" });
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(88, HttpStatusCode.NotFound)]
    [InlineData(89, HttpStatusCode.Forbidden)]
    public async Task ProjectBusinessFailures_AreMappedToDistinctHttpStatuses(int projectId, HttpStatusCode expected)
    {
        var response = await client.GetAsync($"/api/projects/{projectId}");
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task WorkerFault_IsReportedAsBadGateway()
    {
        var response = await client.GetAsync("/api/teams/99");
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.DoesNotContain("private-database-detail", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WorkerWithoutResponse_TimesOutAsServiceUnavailable()
    {
        var response = await client.GetAsync("/api/teams/100");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("private-database-detail", await response.Content.ReadAsStringAsync());
    }
}

public sealed class TeamApiFactory : WebApplicationFactory<Kaban.Api.Features.Teams.Presentation.TeamsController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Auth:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Messaging:RequestTimeoutSeconds", "3");
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<Probe>();
            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<StubTeamsConsumer>();
                RequestTimeout.After(s: 3);
                x.AddRequestClient<CreateTeamRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<ListTeamsRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<GetTeamRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<AddTeamMemberRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<RemoveTeamMemberRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<CreateTeamRoleRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<ChangeMemberRoleRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<DeleteTeamRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<CreateProjectRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<GetProjectRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<ListTeamProjectsRequest>(RequestTimeout.After(s: 3));
                x.AddRequestClient<DeleteProjectRequest>(RequestTimeout.After(s: 3));
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            });
        });
    }
}

public sealed class StubTeamsConsumer :
    IConsumer<CreateTeamRequest>, IConsumer<ListTeamsRequest>, IConsumer<GetTeamRequest>,
    IConsumer<AddTeamMemberRequest>, IConsumer<RemoveTeamMemberRequest>,
    IConsumer<CreateTeamRoleRequest>, IConsumer<ChangeMemberRoleRequest>, IConsumer<DeleteTeamRequest>,
    IConsumer<CreateProjectRequest>, IConsumer<GetProjectRequest>,
    IConsumer<ListTeamProjectsRequest>, IConsumer<DeleteProjectRequest>
{
    private readonly Probe probe;

    public StubTeamsConsumer(Probe probe) => this.probe = probe;

    private void Count() => Interlocked.Increment(ref probe.Calls);

    public async Task Consume(ConsumeContext<CreateTeamRequest> context)
    {
        Count();
        Assert.Equal("New Team", context.Message.Name);
        switch (context.Message.ActorId)
        {
            case 88: await context.RespondAsync(new TeamFailure("invalid_request")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default: await context.RespondAsync(new TeamCreated(55, context.Message.Name)); return;
        }
    }

    public async Task Consume(ConsumeContext<ListTeamsRequest> context)
    {
        Count();
        await context.RespondAsync(new TeamListResult([new TeamSummary(55, "New Team", "owner")]));
    }

    public async Task Consume(ConsumeContext<GetTeamRequest> context)
    {
        Count();
        switch (context.Message.TeamId)
        {
            case 88: await context.RespondAsync(new TeamFailure("team_not_found")); return;
            case 89: await context.RespondAsync(new TeamFailure("not_a_member")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default:
                await context.RespondAsync(new TeamDetails(55, "New Team",
                    [new TeamMemberDetails(7, "User", "user@example.com", "owner")]));
                return;
        }
    }

    public async Task Consume(ConsumeContext<AddTeamMemberRequest> context)
    {
        Count();
        switch (context.Message.TeamId)
        {
            case 88: await context.RespondAsync(new TeamFailure("member_conflict")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default: await context.RespondAsync(new TeamMemberAdded(55, context.Message.UserId, "member")); return;
        }
    }

    public async Task Consume(ConsumeContext<RemoveTeamMemberRequest> context)
    {
        Count();
        switch (context.Message.UserId)
        {
            case 87: await context.RespondAsync(new TeamFailure("member_not_found")); return;
            case 86: await context.RespondAsync(new TeamFailure("last_member")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default: await context.RespondAsync(new TeamMemberRemoved(55, context.Message.UserId)); return;
        }
    }

    public async Task Consume(ConsumeContext<CreateTeamRoleRequest> context)
    {
        Count();
        switch (context.Message.TeamId)
        {
            case 85: await context.RespondAsync(new TeamFailure("role_conflict")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default: await context.RespondAsync(new TeamRoleCreated(55, context.Message.Role)); return;
        }
    }

    public async Task Consume(ConsumeContext<ChangeMemberRoleRequest> context)
    {
        Count();
        switch (context.Message.TeamId)
        {
            case 84: await context.RespondAsync(new TeamFailure("role_not_found")); return;
            case 83: await context.RespondAsync(new TeamFailure("member_not_found")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default: await context.RespondAsync(new MemberRoleChanged(55, context.Message.UserId, context.Message.Role)); return;
        }
    }

    public async Task Consume(ConsumeContext<DeleteTeamRequest> context)
    {
        Count();
        switch (context.Message.TeamId)
        {
            case 88: await context.RespondAsync(new TeamFailure("team_not_found")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default: await context.RespondAsync(new TeamDeleted(55)); return;
        }
    }

    public async Task Consume(ConsumeContext<CreateProjectRequest> context)
    {
        Count();
        Assert.Equal("New Project", context.Message.Name);
        switch (context.Message.TeamId)
        {
            case 88: await context.RespondAsync(new ProjectFailure("team_not_found")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default: await context.RespondAsync(new ProjectCreated(66, context.Message.TeamId, context.Message.Name)); return;
        }
    }

    public async Task Consume(ConsumeContext<GetProjectRequest> context)
    {
        Count();
        switch (context.Message.ProjectId)
        {
            case 88: await context.RespondAsync(new ProjectFailure("project_not_found")); return;
            case 89: await context.RespondAsync(new ProjectFailure("not_a_member")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default: await context.RespondAsync(new ProjectDetails(66, 55, "New Project")); return;
        }
    }

    public async Task Consume(ConsumeContext<ListTeamProjectsRequest> context)
    {
        Count();
        await context.RespondAsync(new ProjectListResult([new ProjectSummary(66, 55, "New Project")]));
    }

    public async Task Consume(ConsumeContext<DeleteProjectRequest> context)
    {
        Count();
        switch (context.Message.ProjectId)
        {
            case 88: await context.RespondAsync(new ProjectFailure("project_not_found")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default: await context.RespondAsync(new ProjectDeleted(66)); return;
        }
    }
}
