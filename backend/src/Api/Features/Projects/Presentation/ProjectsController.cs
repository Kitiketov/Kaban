using System.Globalization;
using Kaban.Api.Features.Projects.Application.CreateProject;
using Kaban.Api.Features.Projects.Application.DeleteProject;
using Kaban.Api.Features.Projects.Application.GetProject;
using Kaban.Api.Features.Projects.Application.ListProjects;
using Kaban.Api.Features.Projects.DTO;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaban.Api.Features.Projects.Presentation;

[ApiController]
[Route("api")]
[Authorize]
public sealed class ProjectsController(IMediator mediator) : ControllerBase
{
    [HttpPost("teams/{teamId:int}/projects")]
    public async Task<IActionResult> Create(int teamId, CreateProjectDto request, CancellationToken ct)
    {
        var project = await mediator.Send(new CreateProjectCommand(ActorId, teamId, request.Name), ct);
        return StatusCode(StatusCodes.Status201Created, project);
    }

    [HttpGet("projects/{projectId:int}")]
    public async Task<IActionResult> Get(int projectId, CancellationToken ct) =>
        Ok(await mediator.Send(new GetProjectQuery(ActorId, projectId), ct));

    [HttpGet("teams/{teamId:int}/projects")]
    public async Task<IActionResult> List(int teamId, CancellationToken ct) =>
        Ok(await mediator.Send(new ListProjectsQuery(ActorId, teamId), ct));

    [HttpDelete("projects/{projectId:int}")]
    public async Task<IActionResult> Delete(int projectId, CancellationToken ct)
    {
        await mediator.Send(new DeleteProjectCommand(ActorId, projectId), ct);
        return NoContent();
    }

    private int ActorId => int.Parse(User.FindFirst("user_id")!.Value, CultureInfo.InvariantCulture);
}
