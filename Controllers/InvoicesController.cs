using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Invoices;

namespace Pos.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Finalizes (saves) a sale invoice. Per BR-13 there is no "draft" state:
    /// the cart lives only in the frontend/session until this single atomic
    /// call creates the invoice directly as Finalized.
    /// </summary>
    [HttpPost("finalize")]
    [ProducesResponseType(typeof(FinalizeInvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinalizeInvoiceResponse>> Finalize(
        [FromBody] FinalizeInvoiceRequest request,
        CancellationToken ct)
    {
        var command = FinalizeInvoiceCommand.FromRequest(request);
        var response = await _mediator.Send(command, ct);
        return Ok(response);
    }
}
