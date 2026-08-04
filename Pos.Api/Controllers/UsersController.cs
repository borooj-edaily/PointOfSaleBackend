using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Users;
using Pos.Api.Security;

namespace Pos.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = Permissions.ManageUsers)]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetUsersQuery(), ct));

    [HttpGet("permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions(CancellationToken ct)
        => Ok(await _mediator.Send(new GetPermissionsQuery(), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUserByIdQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserCommand command, CancellationToken ct)
    {
        var user = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateUserCommand(
            id, body.FullName, body.Username, body.Role, body.NewPassword), ct));

    [HttpPut("{id:int}/permissions")]
    public async Task<ActionResult<UserDto>> SetPermissions(
        int id, SetPermissionsBody body, CancellationToken ct)
        => Ok(await _mediator.Send(
            new SetUserPermissionsCommand(id, body.PermissionIds), ct));

    [HttpPatch("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        await _mediator.Send(new SetUserActiveCommand(id, true), ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _mediator.Send(new SetUserActiveCommand(id, false), ct);
        return NoContent();
    }
}

public sealed record UpdateUserBody(
    string FullName, string Username, string Role, string? NewPassword);
public sealed record SetPermissionsBody(IReadOnlyCollection<int> PermissionIds);
