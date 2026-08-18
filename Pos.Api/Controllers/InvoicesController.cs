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

        // الكاشير دائماً هو صاحب التوكن الحالي،
        // وليس أي ID مرسل من الـ body.
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
    /// Invoice history. A cashier only ever sees their own invoices; a user holding
    /// view_all_invoices (typically Admin) can see every invoice, and may optionally
    /// filter down to one cashier with ?cashierId=.
    /// </summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(
        typeof(ListInvoicesResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ListInvoicesResponse>> List(
        [FromQuery] int? cashierId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new ListInvoicesQuery
        {
            RequestingUserId = CurrentUserId(),
            RequestingUserCanViewAll =
                User.IsInRole("Admin") ||
                User.HasClaim("permission", Permissions.ViewAllInvoices),
            CashierId = cashierId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query, ct);

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
    /// Only users with the ProcessExchange permission can perform an exchange.
    /// </summary>
    [Authorize(Policy = Permissions.ProcessExchange)]
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

    /// <summary>
    /// Debt Notebook: lists invoices recorded as deferred payment ("مين متداين؟").
    /// Defaults to outstanding (unpaid) debts only.
    /// </summary>
    [Authorize(Policy = Permissions.RecordDebt)]
    [HttpGet("debts")]
    [ProducesResponseType(
        typeof(ListDebtsResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ListDebtsResponse>> ListDebts(
        [FromQuery] bool onlyUnpaid = true,
        [FromQuery] string? nickname = null,
        [FromQuery] int? customerId = null,
        CancellationToken ct = default)
    {
        var query = new ListDebtsQuery
        {
            OnlyUnpaid = onlyUnpaid,
            Nickname = nickname,
            CustomerId = customerId
        };

        var response = await _mediator.Send(query, ct);

        return Ok(response);
    }

    /// <summary>Marks a debt invoice as paid ("تسديد الدين").</summary>
    [Authorize(Policy = Permissions.RecordDebt)]
    [HttpPost("{invoiceNumber:int}/pay-debt")]
    [ProducesResponseType(
        typeof(MarkDebtPaidResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<MarkDebtPaidResponse>> PayDebt(
        int invoiceNumber,
        CancellationToken ct)
    {
        var command = new MarkDebtPaidCommand { InvoiceNumber = invoiceNumber };

        var response = await _mediator.Send(command, ct);

        return Ok(response);
    }

    private int CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "The user ID claim is missing.");
        }

        return userId;
    }
}