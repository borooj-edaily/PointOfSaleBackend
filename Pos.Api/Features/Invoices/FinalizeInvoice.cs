using Dapper;
using FluentValidation;
using MediatR;
using Pos.Api.Enums;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Invoices;

public class FinalizeInvoiceRequest
{
    public int CashierId { get; set; }
    public List<InvoiceItemRequest> Items { get; set; } = new();
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }

    // Debt Notebook (v1): when true, the invoice is recorded as deferred
    // payment instead of being settled in cash right now. DebtorNickname is a
    // free-text label (no full customer registry yet) — required when IsDebt.
    public bool IsDebt { get; set; }
    public string? DebtorNickname { get; set; }
}

public class InvoiceItemRequest
{
    public int ProductId { get; set; }
    public string UnitSold { get; set; } = "piece";
    public int Quantity { get; set; }

    // Price Override: lets a cashier holding edit_price charge a different
    // unit price than the catalog price for this one line (damaged item,
    // loyal-customer discount, etc.), with an optional reason for the record.
    public decimal? OverridePrice { get; set; }
    public string? OverrideReason { get; set; }
}

public class FinalizeInvoiceResponse
{
    public int InvoiceId { get; set; }
    public int InvoiceNumber { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDebt { get; set; }
    public string? DebtorNickname { get; set; }
}

public class FinalizeInvoiceCommand : IRequest<FinalizeInvoiceResponse>
{
    public int CashierId { get; set; }
    public List<InvoiceItemRequest> Items { get; set; } = new();
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public bool IsDebt { get; set; }
    public string? DebtorNickname { get; set; }

    public static FinalizeInvoiceCommand FromRequest(FinalizeInvoiceRequest request) => new()
    {
        CashierId = request.CashierId,
        Items = request.Items,
        DiscountType = request.DiscountType,
        DiscountValue = request.DiscountValue,
        IsDebt = request.IsDebt,
        DebtorNickname = request.DebtorNickname
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

            item.RuleFor(i => i.OverridePrice)
                .GreaterThan(0)
                .When(i => i.OverridePrice.HasValue)
                .WithMessage("OverridePrice must be greater than zero.");

            item.RuleFor(i => i.OverrideReason)
                .MaximumLength(255)
                .WithMessage("OverrideReason cannot exceed 255 characters.");
        });

        RuleFor(x => x.DebtorNickname)
            .MaximumLength(100).WithMessage("DebtorNickname cannot exceed 100 characters.");

        When(x => x.IsDebt, () =>
        {
            RuleFor(x => x.DebtorNickname)
                .NotEmpty()
                .WithMessage("DebtorNickname is required when recording an invoice as a debt.");
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

        When(x => x.DiscountType == "fixed" && x.DiscountValue.HasValue, () =>
        {
            RuleFor(x => x.DiscountValue!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Fixed discount value cannot be negative.");
        });

        When(x => x.DiscountType != null, () =>
        {
            RuleFor(x => x.DiscountValue)
                .NotNull().WithMessage("DiscountValue is required when DiscountType is set.");
        });
    }
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
        // BR-01: reject an empty invoice before touching the database at all.
        InvoiceCalculator.EnsureNotEmpty(request.Items.Count);

        using var connection = _database.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Price Override / Debt Notebook: both are permission-gated features.
            // Checked up front, before any row locking, so a cashier without the
            // right permission gets a clean 403 instead of a half-started transaction.
            bool hasPriceOverride = request.Items.Any(i => i.OverridePrice.HasValue);

            if (hasPriceOverride)
            {
                var canOverridePrice = await connection.QuerySingleAsync<int>(
                    @"SELECT COUNT(1) FROM UserPermissions up
                      JOIN Permissions p ON p.Id = up.PermissionId
                      WHERE up.UserId = @CashierId AND p.Name = 'edit_price'",
                    new { request.CashierId },
                    transaction);

                if (canOverridePrice == 0)
                {
                    throw new ForbiddenException(
                        "The user does not have permission to override item prices.");
                }
            }

            if (request.IsDebt)
            {
                var canRecordDebt = await connection.QuerySingleAsync<int>(
                    @"SELECT COUNT(1) FROM UserPermissions up
                      JOIN Permissions p ON p.Id = up.PermissionId
                      WHERE up.UserId = @CashierId AND p.Name = 'record_debt'",
                    new { request.CashierId },
                    transaction);

                if (canRecordDebt == 0)
                {
                    throw new ForbiddenException(
                        "The user does not have permission to record an invoice as a debt.");
                }
            }

            decimal subtotal = 0;
            var lineItems = new List<(int ProductId, string UnitSold, int Quantity, decimal UnitPrice, int QuantityInPieces, decimal LineTotal, int BalanceBeforeInPieces, decimal? OriginalUnitPrice, string? OverrideReason)>();

            // Tracks stock remaining "on paper" as the cart is validated, per product.
            // Without this, two cart lines for the same product (e.g. 6 pieces + 6 pieces
            // on a product with only 10 in stock) would each be checked against the same
            // unchanged StockInPieces value read from the DB and both pass individually,
            // even though together they oversell the product. The actual UPDATE ... SET
            // StockInPieces = StockInPieces - @QuantityInPieces statements below are
            // relative and still apply correctly per line; only the *validation* needed
            // to be aggregated per product.
            var remainingStockInPieces = new Dictionary<int, int>();

            foreach (var item in request.Items)
            {
                // FOR UPDATE locks the product row for the rest of this transaction, so a
                // second, concurrent checkout on the same product has to wait its turn
                // instead of reading stale stock and overselling (BR-02 + BR-03 atomicity).
                // Dapper/MySQL will simply re-use the existing lock if this product was
                // already selected earlier in the loop.
                var stock = await connection.QuerySingleOrDefaultAsync<dynamic>(
                    "SELECT PricePerPiece, PricePerPackage, PiecesPerPackage, StockInPieces, IsActive FROM Products WHERE Id = @ProductId FOR UPDATE",
                    new { item.ProductId },
                    transaction);

                if (stock is null)
                {
                    throw new NotFoundException($"Product {item.ProductId} not found.");
                }

                // BR-05: a discontinued/inactive product stays visible on old invoices,
                // but must not be sellable on a new one.
                if (!(bool)stock.IsActive)
                {
                    throw new BusinessRuleException($"Product {item.ProductId} is inactive and cannot be sold.");
                }

                int piecesPerPackage = (int)(stock.PiecesPerPackage ?? 0);
                int quantityInPieces = InvoiceCalculator.ConvertToBaseUnits(item.UnitSold, item.Quantity, piecesPerPackage);

                // First time we see this product in the cart, seed the running total from
                // the DB value. Every later line for the same product checks against what
                // is "left" after the earlier lines in this same cart, not the raw DB value.
                if (!remainingStockInPieces.TryGetValue(item.ProductId, out var availableInPieces))
                {
                    availableInPieces = (int)stock.StockInPieces;
                }

                InvoiceCalculator.EnsureSufficientStock(item.ProductId, quantityInPieces, availableInPieces);

                int balanceBeforeThisLine = availableInPieces;
                remainingStockInPieces[item.ProductId] = availableInPieces - quantityInPieces;

                decimal unitPrice = InvoiceCalculator.ResolveUnitPrice(
                    item.UnitSold,
                    (decimal?)stock.PricePerPiece,
                    (decimal?)stock.PricePerPackage);

                // Price Override: permission was already verified above for the whole
                // request. The catalog price is kept as OriginalUnitPrice purely for
                // audit/reporting — UnitPriceSnapshot (used everywhere else, incl.
                // returns/exchanges) is always the price actually charged.
                decimal? originalUnitPrice = null;
                string? overrideReason = null;

                if (item.OverridePrice.HasValue)
                {
                    originalUnitPrice = unitPrice;
                    overrideReason = item.OverrideReason;
                    unitPrice = item.OverridePrice.Value;
                }

                // BR-04: unit price is captured here as a snapshot; later changes to
                // Products.PricePerPiece/PricePerPackage never touch this saved invoice.
                decimal lineTotal = InvoiceCalculator.CalculateLineTotal(unitPrice, item.Quantity);
                subtotal += lineTotal;

                lineItems.Add((item.ProductId, item.UnitSold, item.Quantity, unitPrice, quantityInPieces, lineTotal, balanceBeforeThisLine, originalUnitPrice, overrideReason));
            }

            // BR-11: invoice-level discount only; can never push total below zero.
            decimal discountAmount = InvoiceCalculator.CalculateDiscountAmount(subtotal, request.DiscountType, request.DiscountValue);
            decimal total = InvoiceCalculator.CalculateTotal(subtotal, discountAmount);

            var nextNumber = await connection.QuerySingleAsync<int>(
                "SELECT IFNULL(MAX(InvoiceNumber), 0) + 1 FROM Invoices FOR UPDATE",
                transaction: transaction);

            var invoiceId = await connection.QuerySingleAsync<int>(
                @"INSERT INTO Invoices
                    (InvoiceNumber, CashierId, HasReturn, Subtotal, DiscountType, DiscountValue, Total, IsDebt, DebtorNickname, CreatedAt)
                  VALUES
                    (@InvoiceNumber, @CashierId, 0, @Subtotal, @DiscountType, @DiscountValue, @Total, @IsDebt, @DebtorNickname, UTC_TIMESTAMP());
                  SELECT LAST_INSERT_ID();",
                new
                {
                    InvoiceNumber = nextNumber,
                    request.CashierId,
                    Subtotal = subtotal,
                    request.DiscountType,
                    request.DiscountValue,
                    Total = total,
                    request.IsDebt,
                    DebtorNickname = request.IsDebt ? request.DebtorNickname : null
                },
                transaction);

            foreach (var line in lineItems)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO InvoiceItems
                        (InvoiceId, ProductId, UnitSold, Quantity, UnitPriceSnapshot, QuantityInBaseUnits, LineTotal, OriginalUnitPrice, PriceOverrideReason)
                      VALUES
                        (@InvoiceId, @ProductId, @UnitSold, @Quantity, @UnitPrice, @QuantityInPieces, @LineTotal, @OriginalUnitPrice, @OverrideReason)",
                    new
                    {
                        InvoiceId = invoiceId,
                        line.ProductId,
                        line.UnitSold,
                        line.Quantity,
                        UnitPrice = line.UnitPrice,
                        line.QuantityInPieces,
                        line.LineTotal,
                        line.OriginalUnitPrice,
                        line.OverrideReason
                    },
                    transaction);

                await connection.ExecuteAsync(
                    "UPDATE Products SET StockInPieces = StockInPieces - @QuantityInPieces WHERE Id = @ProductId",
                    new { line.ProductId, line.QuantityInPieces },
                    transaction);

                // كل سطر مبيع لازم يترك أثر بسجل StockMovements (نفس نمط Restock/Deduct)،
                // حتى يضل تاريخ المخزون كامل وقابل للتدقيق.
                await connection.ExecuteAsync(
                    @"INSERT INTO StockMovements
                        (ProductId, Type, QuantityInPieces, BalanceBefore, BalanceAfter,
                         Reason, ReferenceInvoiceId, CreatedAt, CreatedByUserId)
                      VALUES
                        (@ProductId, @Type, @QuantityInPieces, @BalanceBefore, @BalanceAfter,
                         NULL, @ReferenceInvoiceId, UTC_TIMESTAMP(6), @CreatedByUserId)",
                    new
                    {
                        line.ProductId,
                        Type = (int)StockMovementType.Sale,
                        line.QuantityInPieces,
                        BalanceBefore = line.BalanceBeforeInPieces,
                        BalanceAfter = line.BalanceBeforeInPieces - line.QuantityInPieces,
                        ReferenceInvoiceId = invoiceId,
                        CreatedByUserId = request.CashierId
                    },
                    transaction);
            }

            transaction.Commit();

            return new FinalizeInvoiceResponse
            {
                InvoiceId = invoiceId,
                InvoiceNumber = nextNumber,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                Total = total,
                CreatedAt = DateTime.UtcNow,
                IsDebt = request.IsDebt,
                DebtorNickname = request.IsDebt ? request.DebtorNickname : null
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