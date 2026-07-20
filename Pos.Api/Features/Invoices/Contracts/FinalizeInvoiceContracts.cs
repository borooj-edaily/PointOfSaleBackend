namespace Pos.Api.Features.Invoices.Contracts;

// ----- Request -----

public class FinalizeInvoiceRequest
{
    public int CashierId { get; set; }
    public List<InvoiceItemRequest> Items { get; set; } = new();
    public string? DiscountType { get; set; }      // "fixed" | "percentage" | null
    public decimal? DiscountValue { get; set; }
}

public class InvoiceItemRequest
{
    public int ProductId { get; set; }
    public string UnitSold { get; set; } = "piece"; // "piece" | "package"
    public int Quantity { get; set; }
}

// ----- Response -----

public class FinalizeInvoiceResponse
{
    public int InvoiceId { get; set; }
    public int InvoiceNumber { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
