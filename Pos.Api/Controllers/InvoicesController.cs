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

    /// <summary>
    /// Looks up a finalized invoice by its human-facing InvoiceNumber, including
    /// each line's already-returned/exchanged quantity so the cashier/exchange
    /// screen knows how much of each line is still eligible for BR-15.
    /// </summary>
    [HttpGet("{invoiceNumber:int}")]
    [ProducesResponseType(typeof(GetInvoiceByNumberResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetInvoiceByNumberResponse>> GetByNumber(
        int invoiceNumber,
        CancellationToken ct)
    {
        var query = new GetInvoiceByNumberQuery { InvoiceNumber = invoiceNumber };
        var response = await _mediator.Send(query, ct);
        return Ok(response);
    }

    /// <summary>
    /// Exchanges one invoice line for a different product: restocks the returned
    /// item, deducts the replacement from stock, and recalculates the invoice
    /// total for the price difference between them (BR-09).
    /// </summary>
    [HttpPost("exchange")]
    [ProducesResponseType(typeof(ExchangeInvoiceItemResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExchangeInvoiceItemResponse>> Exchange(
        [FromBody] ExchangeInvoiceItemRequest request,
        CancellationToken ct)
    {
        var command = ExchangeInvoiceItemCommand.FromRequest(request);
        var response = await _mediator.Send(command, ct);
        return Ok(response);
    }
}