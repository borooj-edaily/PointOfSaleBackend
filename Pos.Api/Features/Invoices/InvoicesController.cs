using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Invoices.Contracts;
using Pos.Api.Features.Invoices.Finalize;

namespace Pos.Api.Features.Invoices;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("finalize")]
    [ProducesResponseType(typeof(FinalizeInvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Finalize([FromBody] FinalizeInvoiceRequest request, CancellationToken ct)
    {
        var command = FinalizeInvoiceCommand.FromRequest(request);
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Finalize), new { id = result.InvoiceId }, result);
    }
}
