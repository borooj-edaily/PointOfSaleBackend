using Dapper;
using FluentValidation;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Invoices;

public class ExchangeInvoiceItemRequest
{
    public int InvoiceItemId { get; set; }
    public int ReturnedQuantity { get; set; }
    public int ReplacementProductId { get; set; }
    public string ReplacementUnitSold { get; set; } = "piece";
    public int ReplacementQuantity { get; set; }
    public int ProcessedBy { get; set; }
    public string? Reason { get; set; }
}

public class ExchangeInvoiceItemResponse
{
    public bool Success { get; set; } = true;
    public int ExchangeId { get; set; }
    public int InvoiceId { get; set; }
    public int InvoiceItemId { get; set; }
    public int ReturnedQuantity { get; set; }
    public int ReplacementProductId { get; set; }
    public int ReplacementQuantity { get; set; }
    public decimal ReturnedItemValue { get; set; }
    public decimal ReplacementItemValue { get; set; }

    // Price difference (BR-09.1): positive => customer owes the difference,
    // negative => a refund is due, zero => even exchange.
    public decimal PriceDifference { get; set; }
    public decimal NewSubtotal { get; set; }
    public decimal NewTotal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExchangeInvoiceItemCommand : IRequest<ExchangeInvoiceItemResponse>
{
    public int InvoiceItemId { get; set; }
    public int ReturnedQuantity { get; set; }
    public int ReplacementProductId { get; set; }
    public string ReplacementUnitSold { get; set; } = "piece";
    public int ReplacementQuantity { get; set; }
    public int ProcessedBy { get; set; }
    public string? Reason { get; set; }

    public static ExchangeInvoiceItemCommand FromRequest(ExchangeInvoiceItemRequest request) => new()
    {
        InvoiceItemId = request.InvoiceItemId,
        ReturnedQuantity = request.ReturnedQuantity,
        ReplacementProductId = request.ReplacementProductId,
        ReplacementUnitSold = request.ReplacementUnitSold,
        ReplacementQuantity = request.ReplacementQuantity,
        ProcessedBy = request.ProcessedBy,
        Reason = request.Reason
    };
}

public class ExchangeInvoiceItemValidator : AbstractValidator<ExchangeInvoiceItemCommand>
{
    public ExchangeInvoiceItemValidator()
    {
        RuleFor(x => x.InvoiceItemId)
            .GreaterThan(0).WithMessage("InvoiceItemId is required.");

        RuleFor(x => x.ReturnedQuantity)
            .GreaterThan(0).WithMessage("ReturnedQuantity must be greater than zero.");

        RuleFor(x => x.ReplacementProductId)
            .GreaterThan(0).WithMessage("ReplacementProductId is required.");

        RuleFor(x => x.ReplacementUnitSold)
            .Must(u => u == "piece" || u == "package")
            .WithMessage("ReplacementUnitSold must be either 'piece' or 'package'.");

        RuleFor(x => x.ReplacementQuantity)
            .GreaterThan(0).WithMessage("ReplacementQuantity must be greater than zero.");

        RuleFor(x => x.ProcessedBy)
            .GreaterThan(0).WithMessage("ProcessedBy (user id) is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(255).WithMessage("Reason cannot exceed 255 characters.");
    }
}

public class ExchangeInvoiceItemHandler : IRequestHandler<ExchangeInvoiceItemCommand, ExchangeInvoiceItemResponse>
{
    private readonly IPosDatabase _database;
    private readonly ILogger<ExchangeInvoiceItemHandler> _logger;

    public ExchangeInvoiceItemHandler(IPosDatabase database, ILogger<ExchangeInvoiceItemHandler> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<ExchangeInvoiceItemResponse> Handle(ExchangeInvoiceItemCommand request, CancellationToken ct)
    {
        using var connection = _database.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // ---------------------------------------------------------------
            // BR-09: the employee must independently hold the 'process_return'
            // permission (checked per-user via UserPermissions, not just
            // inferred from their Role). Checked first, before touching any
            // invoice/stock rows.
            // ---------------------------------------------------------------
            var user = await connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT IsActive FROM Users WHERE Id = @ProcessedBy",
                new { request.ProcessedBy },
                transaction);

            if (user is null)
            {
                throw new NotFoundException($"User {request.ProcessedBy} not found.");
            }

            if (!(bool)user.IsActive)
            {
                throw new ForbiddenException("This user account is inactive and cannot process exchanges.");
            }

            int hasPermission = await connection.QuerySingleAsync<int>(
                @"SELECT COUNT(1) FROM UserPermissions up
                  JOIN Permissions p ON p.Id = up.PermissionId
                  WHERE up.UserId = @ProcessedBy AND p.Name = 'process_return'",
                new { request.ProcessedBy },
                transaction);

            if (hasPermission == 0)
            {
                throw new ForbiddenException(
                    $"User {request.ProcessedBy} does not have permission to process returns/exchanges independently (BR-09).");
            }

            // Lock the invoice item together with its parent invoice so a concurrent
            // return/exchange or a concurrent finalize can't read stale Subtotal/Total values.
            var row = await connection.QuerySingleOrDefaultAsync<dynamic>(
                @"SELECT
                      ii.Id AS InvoiceItemId, ii.InvoiceId, ii.ProductId, ii.UnitSold,
                      ii.Quantity, ii.UnitPriceSnapshot,
                      i.Subtotal, i.DiscountType, i.DiscountValue
                  FROM InvoiceItems ii
                  JOIN Invoices i ON i.Id = ii.InvoiceId
                  WHERE ii.Id = @InvoiceItemId
                  FOR UPDATE",
                new { request.InvoiceItemId },
                transaction);

            if (row is null)
            {
                throw new NotFoundException($"Invoice item {request.InvoiceItemId} not found.");
            }

            int oldProductId = (int)row.ProductId;

            // BR-15: cumulative returns AND exchanges against this line can never
            // exceed what was originally sold on it.
            int alreadyConsumed = await connection.QuerySingleAsync<int>(
                @"SELECT COALESCE(SUM(ReturnedQuantity), 0) FROM InvoiceReturns
                  WHERE InvoiceItemId = @InvoiceItemId AND Type IN ('return', 'exchange')",
                new { request.InvoiceItemId },
                transaction);

            InvoiceCalculator.EnsureExchangeQuantityAllowed(
                alreadyConsumed, request.ReturnedQuantity, (int)row.Quantity);

            // Lock every distinct product row involved up front, in a stable
            // (ascending Id) order -- same convention FinalizeInvoiceHandler uses --
            // so a concurrent exchange/finalize touching the same product(s) can't
            // deadlock or read stale stock.
            var productIdsToLock = new[] { oldProductId, request.ReplacementProductId }
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            var productsById = new Dictionary<int, ProductStockRow>();
            foreach (var productId in productIdsToLock)
            {
                var product = await connection.QuerySingleOrDefaultAsync<ProductStockRow>(
                    @"SELECT Id AS ProductId, IsActive, PricePerPiece, PricePerPackage, PiecesPerPackage, StockInPieces
                      FROM Products
                      WHERE Id = @ProductId
                      FOR UPDATE",
                    new { ProductId = productId },
                    transaction);

                if (product is null)
                {
                    throw new NotFoundException($"Product {productId} not found.");
                }

                productsById[productId] = product;
            }

            // --- Checklist item 1: return the old item (restock it) ---
            var oldProduct = productsById[oldProductId];
            int returnedInPieces = InvoiceCalculator.ConvertToBaseUnits(
                (string)row.UnitSold, request.ReturnedQuantity, oldProduct.PiecesPerPackage);

            await connection.ExecuteAsync(
                "UPDATE Products SET StockInPieces = StockInPieces + @ReturnedInPieces WHERE Id = @ProductId",
                new { ReturnedInPieces = returnedInPieces, ProductId = oldProductId },
                transaction);

            // Keep the in-memory snapshot in sync with the DB write above. This matters
            // when ReplacementProductId == oldProductId (e.g. a quantity-correction
            // "exchange" of the same product): without this, the sufficiency check below
            // would use the stale pre-restock StockInPieces and could reject a valid
            // exchange that the just-restocked quantity actually covers.
            oldProduct.StockInPieces += returnedInPieces;

            decimal returnedItemValue = (decimal)row.UnitPriceSnapshot * request.ReturnedQuantity;

            // --- Checklist item 2: add the replacement item (deduct it from stock) ---
            var replacementProduct = productsById[request.ReplacementProductId];

            if (!replacementProduct.IsActive)
            {
                throw new BusinessRuleException(
                    $"Replacement product {request.ReplacementProductId} is discontinued and cannot be sold.");
            }

            int replacementInPieces = InvoiceCalculator.ConvertToBaseUnits(
                request.ReplacementUnitSold, request.ReplacementQuantity, replacementProduct.PiecesPerPackage);

            InvoiceCalculator.EnsureSufficientStock(
                request.ReplacementProductId, replacementInPieces, replacementProduct.StockInPieces);

            await connection.ExecuteAsync(
                "UPDATE Products SET StockInPieces = StockInPieces - @ReplacementInPieces WHERE Id = @ProductId",
                new { ReplacementInPieces = replacementInPieces, ProductId = request.ReplacementProductId },
                transaction);

            decimal replacementUnitPrice = InvoiceCalculator.ResolveUnitPrice(
                request.ReplacementUnitSold, replacementProduct);
            decimal replacementItemValue = replacementUnitPrice * request.ReplacementQuantity;

            // --- Checklist item 3: auto-recalculate the invoice total (price difference) ---
            decimal priceDifference = replacementItemValue - returnedItemValue;
            decimal newSubtotal = (decimal)row.Subtotal - returnedItemValue + replacementItemValue;

            decimal newDiscountAmount = InvoiceCalculator.RecalculateDiscountAfterAdjustment(
                newSubtotal, (string?)row.DiscountType, (decimal?)row.DiscountValue);
            decimal newTotal = InvoiceCalculator.CalculateTotal(newSubtotal, newDiscountAmount);

            await connection.ExecuteAsync(
                @"UPDATE Invoices
                  SET Subtotal = @NewSubtotal, Total = @NewTotal, HasReturn = 1
                  WHERE Id = @InvoiceId",
                new { NewSubtotal = newSubtotal, NewTotal = newTotal, InvoiceId = (int)row.InvoiceId },
                transaction);

            // Record the exchange itself (audit trail + source of truth for BR-15 above).
            var exchangeId = await connection.QuerySingleAsync<int>(
                @"INSERT INTO InvoiceReturns
                    (InvoiceId, InvoiceItemId, Type, ReturnedQuantity, ReplacementProductId,
                     ReplacementQuantity, ProcessedBy, Reason, CreatedAt)
                  VALUES
                    (@InvoiceId, @InvoiceItemId, 'exchange', @ReturnedQuantity, @ReplacementProductId,
                     @ReplacementQuantity, @ProcessedBy, @Reason, UTC_TIMESTAMP());
                  SELECT LAST_INSERT_ID();",
                new
                {
                    InvoiceId = (int)row.InvoiceId,
                    request.InvoiceItemId,
                    request.ReturnedQuantity,
                    request.ReplacementProductId,
                    request.ReplacementQuantity,
                    request.ProcessedBy,
                    request.Reason
                },
                transaction);

            transaction.Commit();

            return new ExchangeInvoiceItemResponse
            {
                Success = true,
                ExchangeId = exchangeId,
                InvoiceId = (int)row.InvoiceId,
                InvoiceItemId = request.InvoiceItemId,
                ReturnedQuantity = request.ReturnedQuantity,
                ReplacementProductId = request.ReplacementProductId,
                ReplacementQuantity = request.ReplacementQuantity,
                ReturnedItemValue = returnedItemValue,
                ReplacementItemValue = replacementItemValue,
                PriceDifference = priceDifference,
                NewSubtotal = newSubtotal,
                NewTotal = newTotal,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Failed to process exchange for invoice item {InvoiceItemId}", request.InvoiceItemId);
            throw;
        }
    }
}
