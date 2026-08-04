using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Users;

namespace Pos.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var sessionId = User.FindFirstValue(JwtRegisteredClaimNames.Jti)
            ?? User.FindFirstValue("jti")!;
        await _mediator.Send(new LogoutCommand(userId, sessionId), ct);
        return NoContent();
    }
}
