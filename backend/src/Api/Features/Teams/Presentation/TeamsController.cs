using System.Globalization;
using Kaban.Api.Features.Teams.Application.AddTeamMember;
using Kaban.Api.Features.Teams.Application.ChangeMemberRole;
using Kaban.Api.Features.Teams.Application.CreateTeam;
using Kaban.Api.Features.Teams.Application.CreateTeamRole;
using Kaban.Api.Features.Teams.Application.DeleteTeam;
using Kaban.Api.Features.Teams.Application.GetTeam;
using Kaban.Api.Features.Teams.Application.ListTeams;
using Kaban.Api.Features.Teams.Application.RemoveTeamMember;
using Kaban.Api.Features.Teams.DTO;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaban.Api.Features.Teams.Presentation;

[ApiController]
[Route("api/teams")]
[Authorize]
public sealed class TeamsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await mediator.Send(new ListTeamsQuery(ActorId), ct));

    [HttpGet("{teamId:int}")]
    public async Task<IActionResult> Get(int teamId, CancellationToken ct) => 
        Ok(await mediator.Send(new GetTeamQuery(ActorId, teamId), ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateTeamDto request, CancellationToken ct)
    {
        var team = await mediator.Send(new CreateTeamCommand(ActorId, request.Name), ct);
        return StatusCode(StatusCodes.Status201Created, team);
    }

    [HttpDelete("{teamId:int}")]
    public async Task<IActionResult> Delete(int teamId, CancellationToken ct)
    {
        await mediator.Send(new DeleteTeamCommand(ActorId, teamId), ct);
        return NoContent();
    }

    [HttpPost("{teamId:int}/members")]
    public async Task<IActionResult> AddMember(int teamId, AddTeamMemberDto request, CancellationToken ct)
    {
        var member = await mediator.Send(new AddTeamMemberCommand(ActorId, teamId, request.UserId), ct);
        return StatusCode(StatusCodes.Status201Created, member);
    }

    [HttpDelete("{teamId:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int teamId, int userId, CancellationToken ct)
    {
        await mediator.Send(new RemoveTeamMemberCommand(ActorId, teamId, userId), ct);
        return NoContent();
    }

    [HttpPost("{teamId:int}/roles")]
    public async Task<IActionResult> CreateRole(int teamId, CreateTeamRoleDto request, CancellationToken ct)
    {
        var role = await mediator.Send(new CreateTeamRoleCommand(ActorId, teamId, request.Role), ct);
        return StatusCode(StatusCodes.Status201Created, role);
    }

    [HttpPatch("{teamId:int}/members/{userId:int}/roles")]
    public async Task<IActionResult> ChangeRole(int teamId, int userId, ChangeMemberRoleDto request, CancellationToken ct) =>
        Ok(await mediator.Send(new ChangeMemberRoleCommand(ActorId, teamId, userId, request.Role), ct));

    private int ActorId => int.Parse(User.FindFirst("user_id")!.Value, CultureInfo.InvariantCulture);
}
