using Dapper;
using FluentValidation;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Invoices;

public class FinalizeInvoiceRequest
{
    public int CashierId { get; set; }
    public List<InvoiceItemRequest> Items { get; set; } = new();
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
}

public class InvoiceItemRequest
{
    public int ProductId { get; set; }
    public string UnitSold { get; set; } = "piece";
    public int Quantity { get; set; }
}

public class FinalizeInvoiceResponse
{
    // Explicit success flag so this response shape mirrors ApiErrorResponse
    // (which already has Success = false). Any 200 from this endpoint means
    // Success = true; kept explicit here rather than implied by HTTP status
    // alone, since that's what the API contract checklist calls for.
    public bool Success { get; set; } = true;
    public int InvoiceId { get; set; }
    public int InvoiceNumber { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

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

public class FinalizeInvoiceValidator : AbstractValidator<FinalizeInvoiceCommand>
{
    public FinalizeInvoiceValidator()
    {
        RuleFor(x => x.CashierId)
            .GreaterThan(0).WithMessage("CashierId is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Invoice must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");
            item.RuleFor(i => i.UnitSold)
                .Must(u => u == "piece" || u == "package")
                .WithMessage("UnitSold must be either 'piece' or 'package'.");
        });

        RuleFor(x => x.DiscountType)
            .Must(t => t == null || t == "fixed" || t == "percentage")
            .WithMessage("DiscountType must be 'fixed', 'percentage', or null.");

        When(x => x.DiscountType == "percentage" && x.DiscountValue.HasValue, () =>
        {
            RuleFor(x => x.DiscountValue!.Value)
                .InclusiveBetween(0, 100)
                .WithMessage("Percentage discount must be between 0 and 100.");
        });

        When(x => x.DiscountType != null, () =>
        {
            RuleFor(x => x.DiscountValue)
                .NotNull().WithMessage("DiscountValue is required when DiscountType is set.");
        });
    }
}

/// <summary>
/// Read-only projection of a Products row used during Finalize.
/// NOTE: column names (SellBy/IsActive in particular) are assumed to match the
/// Products table owned by Person B — adjust names here if they differ.
/// </summary>
public class ProductStockRow
{
    public int ProductId { get; set; }
    public bool IsActive { get; set; }
    public decimal? PricePerPiece { get; set; }
    public decimal? PricePerPackage { get; set; }
    public int? PiecesPerPackage { get; set; }
    public int StockInPieces { get; set; }
}

public class FinalizeInvoiceHandler : IRequestHandler<FinalizeInvoiceCommand, FinalizeInvoiceResponse>
{
    private readonly IPosDatabase _database;
    private readonly ILogger<FinalizeInvoiceHandler> _logger;

    public FinalizeInvoiceHandler(IPosDatabase database, ILogger<FinalizeInvoiceHandler> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<FinalizeInvoiceResponse> Handle(FinalizeInvoiceCommand request, CancellationToken ct)
    {
        using var connection = _database.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // ---------------------------------------------------------------
            // FIX #2 (race condition): lock every distinct product row up front,
            // in a stable (ascending ProductId) order, so two concurrent
            // Finalize calls can never both read the same "available" stock
            // and both pass validation. Locking in a fixed order also avoids
            // deadlocks between two invoices that share products.
            // ---------------------------------------------------------------
            var distinctProductIds = request.Items
                .Select(i => i.ProductId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            var productsById = new Dictionary<int, ProductStockRow>();

            foreach (var productId in distinctProductIds)
            {
                var product = await connection.QuerySingleOrDefaultAsync<ProductStockRow>(
                    @"SELECT ProductId, IsActive, PricePerPiece, PricePerPackage, PiecesPerPackage, StockInPieces
                      FROM Products
                      WHERE ProductId = @ProductId
                      FOR UPDATE",
                    new { ProductId = productId },
                    transaction);

                if (product is null)
                {
                    throw new NotFoundException($"Product {productId} not found.");
                }

                if (!product.IsActive)
                {
                    throw new BusinessRuleException($"Product {productId} is discontinued and cannot be sold.");
                }

                productsById[productId] = product;
            }

            decimal subtotal = 0;
            var lineItems = new List<(int ProductId, string UnitSold, int Quantity, decimal UnitPrice, int QuantityInPieces, decimal LineTotal)>();

            // Tracks total pieces requested per product ACROSS ALL LINES,
            // so two lines for the same product (e.g. 1 piece + 1 package of
            // the same item) are checked against stock together, not one at a time.
            var requestedPiecesByProduct = new Dictionary<int, int>();

            foreach (var item in request.Items)
            {
                var product = productsById[item.ProductId];

                // -----------------------------------------------------------
                // FIX #3: reject explicitly if the product doesn't actually
                // support the requested sale unit, instead of silently
                // defaulting price to 0 or throwing an unhandled cast error.
                // -----------------------------------------------------------
                if (item.UnitSold == "package")
                {
                    if (product.PricePerPackage is null || product.PiecesPerPackage is null)
                    {
                        throw new BusinessRuleException($"Product {item.ProductId} is not sold by package.");
                    }
                }
                else if (product.PricePerPiece is null)
                {
                    throw new BusinessRuleException($"Product {item.ProductId} is not sold by piece.");
                }

                int quantityInPieces = item.UnitSold == "package"
                    ? item.Quantity * product.PiecesPerPackage!.Value
                    : item.Quantity;

                requestedPiecesByProduct[item.ProductId] =
                    requestedPiecesByProduct.GetValueOrDefault(item.ProductId) + quantityInPieces;

                decimal unitPrice = item.UnitSold == "package"
                    ? product.PricePerPackage!.Value
                    : product.PricePerPiece!.Value;

                decimal lineTotal = unitPrice * item.Quantity;
                subtotal += lineTotal;

                lineItems.Add((item.ProductId, item.UnitSold, item.Quantity, unitPrice, quantityInPieces, lineTotal));
            }

            // ---------------------------------------------------------------
            // FIX #1: validate the AGGREGATED quantity per product against
            // stock (not per-line), now that we know the true total demand.
            // ---------------------------------------------------------------
            foreach (var (productId, totalRequestedPieces) in requestedPiecesByProduct)
            {
                var available = productsById[productId].StockInPieces;
                if (totalRequestedPieces > available)
                {
                    throw new BusinessRuleException(
                        $"Insufficient stock for product {productId}. Available: {available}, requested: {totalRequestedPieces}.");
                }
            }

            decimal discountAmount = request.DiscountType switch
            {
                "fixed" => request.DiscountValue ?? 0,
                "percentage" => subtotal * (request.DiscountValue ?? 0) / 100,
                _ => 0
            };

            // Small bonus fix tied to BR-11: a fixed discount can't be negative
            // (which would increase the total) or exceed the subtotal (which
            // would make total negative).
            if (discountAmount < 0)
            {
                throw new BusinessRuleException("Discount value cannot be negative.");
            }

            if (discountAmount > subtotal)
            {
                throw new BusinessRuleException("Discount cannot exceed the invoice subtotal.");
            }

            decimal total = subtotal - discountAmount;

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
            }

            // Deduct the AGGREGATED quantity per product exactly once, against
            // the row we already locked with FOR UPDATE above — safe even
            // under concurrent Finalize calls on the same product.
            foreach (var (productId, totalRequestedPieces) in requestedPiecesByProduct)
            {
                await connection.ExecuteAsync(
                    "UPDATE Products SET StockInPieces = StockInPieces - @Qty WHERE ProductId = @ProductId",
                    new { Qty = totalRequestedPieces, ProductId = productId },
                    transaction);
            }

            transaction.Commit();

            return new FinalizeInvoiceResponse
            {
                Success = true,
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
