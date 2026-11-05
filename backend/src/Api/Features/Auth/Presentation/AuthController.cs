using System.Globalization;
using Kaban.Api.Features.Auth.Application.Login;
using Kaban.Api.Features.Auth.Application.Refresh;
using Kaban.Api.Features.Auth.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kaban.Api.Features.Auth.Presentation;

public sealed record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("sessions")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new LoginCommand(request.Email, request.Password), ct));

    [Authorize(Policy = AuthRegistration.RefreshPolicy)]
    [HttpPost("tokens/refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct) =>
        Ok(await mediator.Send(new RefreshCommand(
            int.Parse(User.FindFirst("user_id")!.Value, CultureInfo.InvariantCulture)), ct));
}