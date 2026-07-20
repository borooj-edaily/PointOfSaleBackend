using Dapper;
using MediatR;
using Pos.Api.Data;
using Pos.Api.Data.Repositories;
using Pos.Api.Features.Invoices.Contracts;

namespace Pos.Api.Features.Invoices.Finalize;

public class FinalizeInvoiceHandler : IRequestHandler<FinalizeInvoiceCommand, FinalizeInvoiceResponse>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductStockRepository _productStock;
    private readonly ILogger<FinalizeInvoiceHandler> _logger;

    public FinalizeInvoiceHandler(
        IDbConnectionFactory connectionFactory,
        IProductStockRepository productStock,
        ILogger<FinalizeInvoiceHandler> logger)
    {
        _connectionFactory = connectionFactory;
        _productStock = productStock;
        _logger = logger;
    }

    public async Task<FinalizeInvoiceResponse> Handle(FinalizeInvoiceCommand request, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            decimal subtotal = 0;
            var lineItems = new List<(int ProductId, string UnitSold, int Quantity, decimal UnitPrice, int QuantityInPieces, decimal LineTotal)>();

            // BR-02: validate availability and compute prices for every line
            foreach (var item in request.Items)
            {
                var stock = await _productStock.GetStockAsync(item.ProductId)
                    ?? throw new InvalidOperationException($"Product {item.ProductId} not found.");

                int quantityInPieces = item.UnitSold == "package"
                    ? item.Quantity * (stock.PiecesPerPackage ?? throw new InvalidOperationException(
                        $"Product {item.ProductId} has no package size configured."))
                    : item.Quantity;

                if (quantityInPieces > stock.StockInPieces)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for product {item.ProductId}. Available: {stock.StockInPieces}, requested: {quantityInPieces}.");
                }

                decimal unitPrice = item.UnitSold == "package"
                    ? stock.PricePerPackage ?? throw new InvalidOperationException($"Product {item.ProductId} has no package price.")
                    : stock.PricePerPiece;

                decimal lineTotal = unitPrice * item.Quantity;
                subtotal += lineTotal;

                lineItems.Add((item.ProductId, item.UnitSold, item.Quantity, unitPrice, quantityInPieces, lineTotal));
            }

            // BR-11: invoice-level discount only
            decimal discountAmount = request.DiscountType switch {
    "fixed" => request.DiscountValue ?? 0,
    "percentage" => subtotal * (request.DiscountValue ?? 0) / 100,
    _ => 0
};
decimal total = subtotal - discountAmount;
            // BR-19: invoice number reserved atomically, only at finalize time.
            // FOR UPDATE locks the aggregate so two concurrent finalizes can't
            // read the same MAX() value.
            var nextNumber = await connection.QuerySingleAsync<int>(
                "SELECT IFNULL(MAX(InvoiceNumber), 0) + 1 FROM Invoices FOR UPDATE",
                transaction: transaction);

            var invoiceId = await connection.QuerySingleAsync<int>(
                @"INSERT INTO Invoices
                    (InvoiceNumber, CashierId, HasReturn, Subtotal, DiscountType, DiscountValue, Total, CreatedAt)
                  VALUES
                    (@InvoiceNumber, @CashierId, 0, @Subtotal, @DiscountType, @DiscountValue, @Total, UTC_TIMESTAMP());
                  SELECT LAST_INSERT_ID();",
                new
                {
                    InvoiceNumber = nextNumber,
                    request.CashierId,
                    Subtotal = subtotal,
                    request.DiscountType,
                    request.DiscountValue,
                    Total = total
                },
                transaction);

            foreach (var line in lineItems)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO InvoiceItems
                        (InvoiceId, ProductId, UnitSold, Quantity, UnitPriceSnapshot, QuantityInBaseUnits, LineTotal)
                      VALUES
                        (@InvoiceId, @ProductId, @UnitSold, @Quantity, @UnitPrice, @QuantityInPieces, @LineTotal)",
                    new
                    {
                        InvoiceId = invoiceId,
                        line.ProductId,
                        line.UnitSold,
                        line.Quantity,
                        UnitPrice = line.UnitPrice,
                        line.QuantityInPieces,
                        line.LineTotal
                    },
                    transaction);

                // BR-03: deduct stock as part of the same atomic operation.
                // IMPORTANT (card 7): the placeholder repository updates an
                // in-memory dictionary, so it can't actually roll back with
                // `transaction`. When swapping in Person B's real
                // implementation, pass `connection`/`transaction` into it so
                // the UPDATE Products SET StockInPieces=... runs inside this
                // same transaction and rolls back on failure too.
                await _productStock.DecrementStockAsync(line.ProductId, line.QuantityInPieces);
            }

            transaction.Commit();

            return new FinalizeInvoiceResponse
            {
                InvoiceId = invoiceId,
                InvoiceNumber = nextNumber,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                Total = total,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Failed to finalize invoice for cashier {CashierId}", request.CashierId);
            throw;
        }
    }
}
