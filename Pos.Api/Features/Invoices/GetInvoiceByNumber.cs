using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Invoices;

public class InvoiceItemDto
{
    public int InvoiceItemId { get; set; }
    public int ProductId { get; set; }
    public string UnitSold { get; set; } = "piece";
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal LineTotal { get; set; }

    // BR-15 support: how many units of this line were already returned/exchanged,
    // and how many are still eligible to be returned/exchanged.
    public int AlreadyReturnedQuantity { get; set; }
    public int ReturnableQuantity { get; set; }

    // Price Override: set only when this line's price was overridden at checkout.
    public decimal? OriginalUnitPrice { get; set; }
    public string? PriceOverrideReason { get; set; }
}

public class GetInvoiceByNumberResponse
{
    public int InvoiceId { get; set; }
    public int InvoiceNumber { get; set; }
    public int CashierId { get; set; }
    public bool HasReturn { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();

    // Debt Notebook (v1)
    public bool IsDebt { get; set; }
    public string? DebtorNickname { get; set; }
    public DateTime? DebtPaidAt { get; set; }
}

public class GetInvoiceByNumberQuery : IRequest<GetInvoiceByNumberResponse>
{
    public int InvoiceNumber { get; set; }
}

/// <summary>Read-only projection of the Invoices row, used only inside this handler.</summary>
public class InvoiceHeaderRow
{
    public int InvoiceId { get; set; }
    public int InvoiceNumber { get; set; }
    public int CashierId { get; set; }
    public bool HasReturn { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDebt { get; set; }
    public string? DebtorNickname { get; set; }
    public DateTime? DebtPaidAt { get; set; }
}

/// <summary>Read-only projection of an InvoiceItems row, joined with its returned/exchanged total.</summary>
public class InvoiceItemRow
{
    public int InvoiceItemId { get; set; }
    public int ProductId { get; set; }
    public string UnitSold { get; set; } = "piece";
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal LineTotal { get; set; }
    public int AlreadyReturnedQuantity { get; set; }
    public decimal? OriginalUnitPrice { get; set; }
    public string? PriceOverrideReason { get; set; }
}

public class GetInvoiceByNumberHandler : IRequestHandler<GetInvoiceByNumberQuery, GetInvoiceByNumberResponse>
{
    private readonly IPosDatabase _database;

    public GetInvoiceByNumberHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<GetInvoiceByNumberResponse> Handle(GetInvoiceByNumberQuery request, CancellationToken ct)
    {
        using var connection = _database.Open();

        var invoice = await connection.QuerySingleOrDefaultAsync<InvoiceHeaderRow>(
            @"SELECT
                  Id AS InvoiceId, InvoiceNumber, CashierId, HasReturn,
                  Subtotal, Total, CreatedAt, IsDebt, DebtorNickname, DebtPaidAt
              FROM Invoices
              WHERE InvoiceNumber = @InvoiceNumber",
            new { request.InvoiceNumber });

        if (invoice is null)
        {
            throw new NotFoundException($"Invoice {request.InvoiceNumber} not found.");
        }

        // For each line, sum whatever was already consumed by prior returns/exchanges
        // (BR-15), so the frontend can show/enforce the remaining returnable quantity.
        var itemRows = await connection.QueryAsync<InvoiceItemRow>(
            @"SELECT
                  ii.Id AS InvoiceItemId, ii.ProductId, ii.UnitSold, ii.Quantity,
                  ii.UnitPriceSnapshot, ii.LineTotal,
                  ii.OriginalUnitPrice, ii.PriceOverrideReason,
                  COALESCE((
                      SELECT SUM(ir.ReturnedQuantity)
                      FROM InvoiceReturns ir
                      WHERE ir.InvoiceItemId = ii.Id AND ir.Type IN ('return', 'exchange')
                  ), 0) AS AlreadyReturnedQuantity
              FROM InvoiceItems ii
              WHERE ii.InvoiceId = @InvoiceId",
            new { InvoiceId = invoice.InvoiceId });

        return new GetInvoiceByNumberResponse
        {
            InvoiceId = invoice.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            CashierId = invoice.CashierId,
            HasReturn = invoice.HasReturn,
            Subtotal = invoice.Subtotal,
            Total = invoice.Total,
            CreatedAt = invoice.CreatedAt,
            IsDebt = invoice.IsDebt,
            DebtorNickname = invoice.DebtorNickname,
            DebtPaidAt = invoice.DebtPaidAt,
            Items = itemRows.Select(i => new InvoiceItemDto
            {
                InvoiceItemId = i.InvoiceItemId,
                ProductId = i.ProductId,
                UnitSold = i.UnitSold,
                Quantity = i.Quantity,
                UnitPriceSnapshot = i.UnitPriceSnapshot,
                LineTotal = i.LineTotal,
                AlreadyReturnedQuantity = i.AlreadyReturnedQuantity,
                ReturnableQuantity = i.Quantity - i.AlreadyReturnedQuantity,
                OriginalUnitPrice = i.OriginalUnitPrice,
                PriceOverrideReason = i.PriceOverrideReason
            }).ToList()
        };
    }
}