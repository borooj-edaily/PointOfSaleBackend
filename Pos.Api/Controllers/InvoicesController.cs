using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Invoices;
using Pos.Api.Security;

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

    [Authorize(Policy = Permissions.CreateInvoice)]
    [HttpPost("finalize")]
    public async Task<IActionResult> Finalize(
        [FromBody] FinalizeInvoiceRequest request,
        CancellationToken ct)
    {
        var command = FinalizeInvoiceCommand.FromRequest(request);

        // الكاشير دايماً هو صاحب التوكن الحالي، مش أي id مبعوت بالـ body — هيك ما
        // يقدر حدا يسجّل فاتورة باسم كاشير تاني.
        command.CashierId = CurrentUserId();

        var response = await _mediator.Send(command, ct);

        return Ok(response);
    }

    [Authorize(Policy = Permissions.ProcessReturn)]
    [HttpPost("returns")]
    public async Task<IActionResult> Return(
        [FromBody] ReturnInvoiceItemRequest request,
        CancellationToken ct)
    {
        var command = ReturnInvoiceItemCommand.FromRequest(request);
        command.ProcessedBy = CurrentUserId();

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
    [Authorize(Policy = Permissions.ProcessReturn)]
    [HttpPost("exchange")]
    [ProducesResponseType(
        typeof(ExchangeInvoiceItemResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ExchangeInvoiceItemResponse>> Exchange(
        [FromBody] ExchangeInvoiceItemRequest request,
        CancellationToken ct)
    {
        var command = ExchangeInvoiceItemCommand.FromRequest(request);
        command.ProcessedBy = CurrentUserId();

        var response = await _mediator.Send(command, ct);

        return Ok(response);
    }

    private int CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("The user ID claim is missing.");

        return userId;
    }
}