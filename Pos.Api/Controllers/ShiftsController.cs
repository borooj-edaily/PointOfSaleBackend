using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Shifts;
using Pos.Api.Security;

namespace Pos.Api.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize]
public sealed class ShiftsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShiftsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("check-in")]
    public async Task<ActionResult<ShiftDto>> CheckIn(
        CancellationToken ct)
    {
        var shift = await _mediator.Send(
            new CheckInCommand(CurrentUserId()),
            ct);

        return Ok(shift);
    }

    [HttpPost("check-out")]
    public async Task<ActionResult<ShiftDto>> CheckOut(
        CancellationToken ct)
    {
        var shift = await _mediator.Send(
            new CheckOutCommand(CurrentUserId()),
            ct);

        return Ok(shift);
    }

    [HttpGet("current")]
    public async Task<ActionResult<ShiftDto?>> Current(
        CancellationToken ct)
    {
        var shift = await _mediator.Send(
            new GetCurrentShiftQuery(CurrentUserId()),
            ct);

        return Ok(shift);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<ShiftDto>>> MyShifts(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var shifts = await _mediator.Send(
            new GetMyShiftsQuery(
                CurrentUserId(),
                from,
                to),
            ct);

        return Ok(shifts);
    }

    [HttpGet("report")]
    [Authorize(Policy = Permissions.ViewReports)]
    public async Task<ActionResult<ShiftReportResponse>> Report(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int? userId,
        CancellationToken ct)
    {
        var report = await _mediator.Send(
            new GetShiftReportQuery(from, to, userId),
            ct);

        return Ok(report);
    }

    private int CurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var userId))
            throw new UnauthorizedAccessException(
                "The user ID claim is missing.");

        return userId;
    }
}