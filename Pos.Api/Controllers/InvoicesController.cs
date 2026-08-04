using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Invoices;

namespace Pos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("finalize")]
    public async Task<IActionResult> Finalize(
        [FromBody] FinalizeInvoiceRequest request,
        CancellationToken ct)
    {
        var command = FinalizeInvoiceCommand.FromRequest(request);
        var response = await _mediator.Send(command, ct);

        return Ok(response);
    }

    [HttpPost("returns")]
    public async Task<IActionResult> Return(
        [FromBody] ReturnInvoiceItemRequest request,
        CancellationToken ct)
    {
        var command = ReturnInvoiceItemCommand.FromRequest(request);
        var response = await _mediator.Send(command, ct);

        return Ok(response);
    }

    /// <summary>
    /// Looks up a finalized invoice by its human-facing InvoiceNumber,
    /// including each line's already-returned/exchanged quantity.
    /// </summary>
    [HttpGet("{invoiceNumber:int}")]
    [ProducesResponseType(
        typeof(GetInvoiceByNumberResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<GetInvoiceByNumberResponse>> GetByNumber(
        int invoiceNumber,
        CancellationToken ct)
    {
        var query = new GetInvoiceByNumberQuery
        {
            InvoiceNumber = invoiceNumber
        };

        var response = await _mediator.Send(query, ct);

        return Ok(response);
    }

    /// <summary>
    /// Exchanges one invoice line for another product.
    /// </summary>
    [HttpPost("exchange")]
    [ProducesResponseType(
        typeof(ExchangeInvoiceItemResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ExchangeInvoiceItemResponse>> Exchange(
        [FromBody] ExchangeInvoiceItemRequest request,
        CancellationToken ct)
    {
        var command = ExchangeInvoiceItemCommand.FromRequest(request);
        var response = await _mediator.Send(command, ct);

        return Ok(response);
    }
}