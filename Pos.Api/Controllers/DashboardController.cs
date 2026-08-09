using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Dashboard.GetStats;
using Pos.Api.Security;

namespace Pos.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = Permissions.ViewReports)]
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var result = await _mediator.Send(new GetDashboardStatsQuery());
            return Ok(result);
        }
    }
}