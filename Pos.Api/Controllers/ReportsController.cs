using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Reports;
using Pos.Api.Security;

namespace Pos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Sales summary for a date range: totals, daily breakdown, top products and
    /// sales-by-cashier. Defaults to the last 30 days when no range is supplied.
    /// </summary>
    [Authorize(Policy = Permissions.ViewReports)]
    [HttpGet("sales")]
    [ProducesResponseType(typeof(SalesReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SalesReportResponse>> Sales(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        var response = await _mediator.Send(
            new SalesReportQuery { FromDate = fromDate, ToDate = toDate },
            ct);

        return Ok(response);
    }
}