using System.Globalization;
using Kaban.Api.Features.Users.Application.GetMe;
using Kaban.Api.Features.Users.Application.Register;
using Kaban.Api.Features.Users.Application.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kaban.Api.Features.Users.Presentation;

public sealed record RegisterUserRequest(string Email, string Name, string Password);

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken ct)
    {
        await mediator.Send(new RegisterUserCommand(request.Email, request.Name, request.Password), ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        Ok(await mediator.Send(new GetMeQuery(ActorId), ct));

    [HttpGet]
    public async Task<IActionResult>
        Search([FromQuery] string query, CancellationToken ct, [FromQuery] int limit = 10) =>
        Ok(await mediator.Send(new SearchUsersQuery(ActorId, query, limit), ct));

    private int ActorId => int.Parse(User.FindFirst("user_id")!.Value, CultureInfo.InvariantCulture);
}