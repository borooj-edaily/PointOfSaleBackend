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
    public decimal PriceDifference { get; set; }
    public decimal NewSubtotal { get; set; }
    public decimal NewTotal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExchangeInvoiceItemCommand
    : IRequest<ExchangeInvoiceItemResponse>
{
    public int InvoiceItemId { get; set; }
    public int ReturnedQuantity { get; set; }
    public int ReplacementProductId { get; set; }
    public string ReplacementUnitSold { get; set; } = "piece";
    public int ReplacementQuantity { get; set; }
    public int ProcessedBy { get; set; }
    public string? Reason { get; set; }

    public static ExchangeInvoiceItemCommand FromRequest(
        ExchangeInvoiceItemRequest request)
    {
        return new ExchangeInvoiceItemCommand
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
}

public class ExchangeInvoiceItemValidator
    : AbstractValidator<ExchangeInvoiceItemCommand>
{
    public ExchangeInvoiceItemValidator()
    {
        RuleFor(x => x.InvoiceItemId)
            .GreaterThan(0);

        RuleFor(x => x.ReturnedQuantity)
            .GreaterThan(0);

        RuleFor(x => x.ReplacementProductId)
            .GreaterThan(0);

        RuleFor(x => x.ReplacementUnitSold)
            .Must(unit => unit is "piece" or "package")
            .WithMessage(
                "ReplacementUnitSold must be either 'piece' or 'package'.");

        RuleFor(x => x.ReplacementQuantity)
            .GreaterThan(0);

        RuleFor(x => x.ProcessedBy)
            .GreaterThan(0);

        RuleFor(x => x.Reason)
            .MaximumLength(255);
    }
}

public sealed class ProductStockRow
{
    public int ProductId { get; set; }
    public bool IsActive { get; set; }
    public decimal? PricePerPiece { get; set; }
    public decimal? PricePerPackage { get; set; }
    public int PiecesPerPackage { get; set; }
    public int StockInPieces { get; set; }
}

public class ExchangeInvoiceItemHandler
    : IRequestHandler<
        ExchangeInvoiceItemCommand,
        ExchangeInvoiceItemResponse>
{
    private readonly IPosDatabase _database;
    private readonly ILogger<ExchangeInvoiceItemHandler> _logger;

    public ExchangeInvoiceItemHandler(
        IPosDatabase database,
        ILogger<ExchangeInvoiceItemHandler> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<ExchangeInvoiceItemResponse> Handle(
        ExchangeInvoiceItemCommand request,
        CancellationToken cancellationToken)
    {
        using var connection = _database.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var user = await connection.QuerySingleOrDefaultAsync<dynamic>(
                """
                SELECT IsActive
                FROM Users
                WHERE Id = @ProcessedBy;
                """,
                new { request.ProcessedBy },
                transaction);

            if (user is null)
            {
                throw new NotFoundException(
                    $"User {request.ProcessedBy} not found.");
            }

            if (!(bool)user.IsActive)
            {
                throw new ForbiddenException(
                    "This user account is inactive and cannot process exchanges.");
            }

            var hasPermission =
                await connection.QuerySingleAsync<int>(
                    """
                    SELECT COUNT(1)
                    FROM UserPermissions up
                    JOIN Permissions p
                        ON p.Id = up.PermissionId
                    WHERE up.UserId = @ProcessedBy
                      AND p.Name = 'process_return';
                    """,
                    new { request.ProcessedBy },
                    transaction);

            if (hasPermission == 0)
            {
                throw new ForbiddenException(
                    "The user does not have permission to process exchanges.");
            }

            var invoiceItem =
                await connection.QuerySingleOrDefaultAsync<dynamic>(
                    """
                    SELECT
                        ii.Id AS InvoiceItemId,
                        ii.InvoiceId,
                        ii.ProductId,
                        ii.UnitSold,
                        ii.Quantity,
                        ii.UnitPriceSnapshot,
                        i.Subtotal,
                        i.DiscountType,
                        i.DiscountValue
                    FROM InvoiceItems ii
                    JOIN Invoices i
                        ON i.Id = ii.InvoiceId
                    WHERE ii.Id = @InvoiceItemId
                    FOR UPDATE;
                    """,
                    new { request.InvoiceItemId },
                    transaction);

            if (invoiceItem is null)
            {
                throw new NotFoundException(
                    $"Invoice item {request.InvoiceItemId} not found.");
            }

            var oldProductId = (int)invoiceItem.ProductId;

            var alreadyReturned =
                await connection.QuerySingleAsync<int>(
                    """
                    SELECT COALESCE(SUM(ReturnedQuantity), 0)
                    FROM InvoiceReturns
                    WHERE InvoiceItemId = @InvoiceItemId
                      AND Type IN ('return', 'exchange');
                    """,
                    new { request.InvoiceItemId },
                    transaction);

            InvoiceCalculator.EnsureReturnQuantityAllowed(
                alreadyReturned,
                request.ReturnedQuantity,
                (int)invoiceItem.Quantity);

            var productIds = new[]
                {
                    oldProductId,
                    request.ReplacementProductId
                }
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            var products =
                new Dictionary<int, ProductStockRow>();

            foreach (var productId in productIds)
            {
                var product =
                    await connection
                        .QuerySingleOrDefaultAsync<ProductStockRow>(
                            """
                            SELECT
                                Id AS ProductId,
                                IsActive,
                                PricePerPiece,
                                PricePerPackage,
                                PiecesPerPackage,
                                StockInPieces
                            FROM Products
                            WHERE Id = @ProductId
                            FOR UPDATE;
                            """,
                            new { ProductId = productId },
                            transaction);

                if (product is null)
                {
                    throw new NotFoundException(
                        $"Product {productId} not found.");
                }

                products[productId] = product;
            }

            var oldProduct = products[oldProductId];

            var returnedInPieces =
                InvoiceCalculator.ConvertToBaseUnits(
                    (string)invoiceItem.UnitSold,
                    request.ReturnedQuantity,
                    oldProduct.PiecesPerPackage);

            await connection.ExecuteAsync(
                """
                UPDATE Products
                SET StockInPieces =
                    StockInPieces + @ReturnedInPieces
                WHERE Id = @ProductId;
                """,
                new
                {
                    ReturnedInPieces = returnedInPieces,
                    ProductId = oldProductId
                },
                transaction);

            oldProduct.StockInPieces += returnedInPieces;

            var returnedItemValue =
                (decimal)invoiceItem.UnitPriceSnapshot *
                request.ReturnedQuantity;

            var replacementProduct =
                products[request.ReplacementProductId];

            if (!replacementProduct.IsActive)
            {
                throw new BusinessRuleException(
                    $"Replacement product {request.ReplacementProductId} is inactive.");
            }

            var replacementInPieces =
                InvoiceCalculator.ConvertToBaseUnits(
                    request.ReplacementUnitSold,
                    request.ReplacementQuantity,
                    replacementProduct.PiecesPerPackage);

            InvoiceCalculator.EnsureSufficientStock(
                request.ReplacementProductId,
                replacementInPieces,
                replacementProduct.StockInPieces);

            await connection.ExecuteAsync(
                """
                UPDATE Products
                SET StockInPieces =
                    StockInPieces - @ReplacementInPieces
                WHERE Id = @ProductId;
                """,
                new
                {
                    ReplacementInPieces = replacementInPieces,
                    ProductId = request.ReplacementProductId
                },
                transaction);

            var replacementUnitPrice =
                InvoiceCalculator.ResolveUnitPrice(
                    request.ReplacementUnitSold,
                    replacementProduct.PricePerPiece,
                    replacementProduct.PricePerPackage);

            var replacementItemValue =
                replacementUnitPrice *
                request.ReplacementQuantity;

            var priceDifference =
                replacementItemValue -
                returnedItemValue;

            var newSubtotal =
                (decimal)invoiceItem.Subtotal -
                returnedItemValue +
                replacementItemValue;

            var newDiscountAmount =
                InvoiceCalculator.RecalculateDiscountAfterReturn(
                    newSubtotal,
                    (string?)invoiceItem.DiscountType,
                    (decimal?)invoiceItem.DiscountValue);

            var newTotal =
                InvoiceCalculator.CalculateTotal(
                    newSubtotal,
                    newDiscountAmount);

            await connection.ExecuteAsync(
                """
                UPDATE Invoices
                SET Subtotal = @NewSubtotal,
                    Total = @NewTotal,
                    HasReturn = 1
                WHERE Id = @InvoiceId;
                """,
                new
                {
                    NewSubtotal = newSubtotal,
                    NewTotal = newTotal,
                    InvoiceId = (int)invoiceItem.InvoiceId
                },
                transaction);

            var exchangeId =
                await connection.QuerySingleAsync<int>(
                    """
                    INSERT INTO InvoiceReturns
                    (
                        InvoiceId,
                        InvoiceItemId,
                        Type,
                        ReturnedQuantity,
                        ReplacementProductId,
                        ReplacementQuantity,
                        ProcessedBy,
                        Reason,
                        CreatedAt
                    )
                    VALUES
                    (
                        @InvoiceId,
                        @InvoiceItemId,
                        'exchange',
                        @ReturnedQuantity,
                        @ReplacementProductId,
                        @ReplacementQuantity,
                        @ProcessedBy,
                        @Reason,
                        UTC_TIMESTAMP()
                    );

                    SELECT LAST_INSERT_ID();
                    """,
                    new
                    {
                        InvoiceId =
                            (int)invoiceItem.InvoiceId,
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
                InvoiceId =
                    (int)invoiceItem.InvoiceId,
                InvoiceItemId =
                    request.InvoiceItemId,
                ReturnedQuantity =
                    request.ReturnedQuantity,
                ReplacementProductId =
                    request.ReplacementProductId,
                ReplacementQuantity =
                    request.ReplacementQuantity,
                ReturnedItemValue =
                    returnedItemValue,
                ReplacementItemValue =
                    replacementItemValue,
                PriceDifference =
                    priceDifference,
                NewSubtotal =
                    newSubtotal,
                NewTotal =
                    newTotal,
                CreatedAt =
                    DateTime.UtcNow
            };
        }
        catch (Exception exception)
        {
            transaction.Rollback();

            _logger.LogError(
                exception,
                "Failed to process exchange for invoice item {InvoiceItemId}",
                request.InvoiceItemId);

            throw;
        }
    }
}