using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.AuditLogs;
using Pos.Api.Security;

namespace Pos.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = Permissions.ViewAuditLog)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogResponse>> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? userId,
        [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAuditLogsQuery(
                from,
                to,
                userId,
                action,
                entity,
                page,
                pageSize),
            ct);

        return Ok(result);
    }
}