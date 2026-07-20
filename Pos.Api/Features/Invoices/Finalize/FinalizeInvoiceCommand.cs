using MediatR;
using Pos.Api.Features.Invoices.Contracts;

namespace Pos.Api.Features.Invoices.Finalize;

public class FinalizeInvoiceCommand : IRequest<FinalizeInvoiceResponse>
{
    public int CashierId { get; set; }
    public List<InvoiceItemRequest> Items { get; set; } = new();
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }

    public static FinalizeInvoiceCommand FromRequest(FinalizeInvoiceRequest request) => new()
    {
        CashierId = request.CashierId,
        Items = request.Items,
        DiscountType = request.DiscountType,
        DiscountValue = request.DiscountValue
    };
}
