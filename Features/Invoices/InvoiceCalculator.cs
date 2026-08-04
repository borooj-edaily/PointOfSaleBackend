using Pos.Api.Exceptions;

namespace Pos.Api.Features.Invoices;

public record InvoiceLineResult(
    int ProductId,
    string UnitSold,
    int Quantity,
    decimal UnitPrice,
    int QuantityInPieces,
    decimal LineTotal);

/// <summary>
/// Pure, DB-free invoice math and business-rule checks, extracted out of
/// FinalizeInvoiceHandler specifically so they can be unit tested without a
/// real database connection.
/// </summary>
public static class InvoiceCalculator
{
    /// <summary>
    /// BR-04: the unit price is captured from the product row passed in
    /// (the state read at the moment of sale) rather than re-read later, so
    /// a price change on the product afterwards never affects an
    /// already-finalized invoice.
    /// </summary>
    public static InvoiceLineResult BuildLineItem(InvoiceItemRequest item, ProductStockRow product)
    {
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

        decimal unitPrice = item.UnitSold == "package"
            ? product.PricePerPackage!.Value
            : product.PricePerPiece!.Value;

        decimal lineTotal = unitPrice * item.Quantity;

        return new InvoiceLineResult(item.ProductId, item.UnitSold, item.Quantity, unitPrice, quantityInPieces, lineTotal);
    }

    /// <summary>
    /// BR-02: throws if the aggregated requested quantity (summed across all
    /// lines for the same product) exceeds the stock available for it.
    /// </summary>
    public static void ValidateStock(
        IReadOnlyDictionary<int, int> requestedPiecesByProduct,
        IReadOnlyDictionary<int, ProductStockRow> productsById)
    {
        foreach (var (productId, totalRequestedPieces) in requestedPiecesByProduct)
        {
            var available = productsById[productId].StockInPieces;
            if (totalRequestedPieces > available)
            {
                throw new BusinessRuleException(
                    $"Insufficient stock for product {productId}. Available: {available}, requested: {totalRequestedPieces}.");
            }
        }
    }

    /// <summary>
    /// BR-11: calculates the discount amount for a subtotal, rejecting a
    /// discount that is negative or that would exceed the subtotal.
    /// </summary>
    public static decimal CalculateDiscountAmount(decimal subtotal, string? discountType, decimal? discountValue)
    {
        decimal discountAmount = discountType switch
        {
            "fixed" => discountValue ?? 0,
            "percentage" => subtotal * (discountValue ?? 0) / 100,
            _ => 0
        };

        if (discountAmount < 0)
        {
            throw new BusinessRuleException("Discount value cannot be negative.");
        }

        if (discountAmount > subtotal)
        {
            throw new BusinessRuleException("Discount cannot exceed the invoice subtotal.");
        }

        return discountAmount;
    }
}
