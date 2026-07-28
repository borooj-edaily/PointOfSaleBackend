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

    // ------------------------------------------------------------------
    // Below: helpers added for the Return/Exchange feature. Kept in this
    // same static class (rather than a new one) since they're the same
    // kind of pure, DB-free invoice math as everything above, and the
    // Exchange handler already depends on ProductStockRow/BusinessRuleException
    // from this file.
    // ------------------------------------------------------------------

    /// <summary>
    /// BR-02.1: stock is always tracked in pieces regardless of how a product is
    /// sold. Converts a piece/package quantity into the base "pieces" unit.
    /// </summary>
    public static int ConvertToBaseUnits(string unitSold, int quantity, int? piecesPerPackage)
    {
        if (unitSold == "package")
        {
            if (piecesPerPackage is null or <= 0)
            {
                throw new BusinessRuleException(
                    "This product is not configured to be sold by package (missing PiecesPerPackage).");
            }

            return quantity * piecesPerPackage.Value;
        }

        return quantity;
    }

    /// <summary>Picks the correct price (BR-04) for the unit a replacement item is sold in.</summary>
    public static decimal ResolveUnitPrice(string unitSold, ProductStockRow product)
    {
        if (unitSold == "package")
        {
            return product.PricePerPackage
                ?? throw new BusinessRuleException($"Product {product.ProductId} has no package price configured.");
        }

        return product.PricePerPiece
            ?? throw new BusinessRuleException($"Product {product.ProductId} has no piece price configured.");
    }

    /// <summary>BR-02: a single-product stock check (used outside the aggregated Finalize path).</summary>
    public static void EnsureSufficientStock(int productId, int requestedInPieces, int availableInPieces)
    {
        if (requestedInPieces > availableInPieces)
        {
            throw new BusinessRuleException(
                $"Insufficient stock for product {productId}. Available: {availableInPieces}, requested: {requestedInPieces}.");
        }
    }

    /// <summary>
    /// BR-15: the cumulative quantity already returned/exchanged for a line (plus
    /// this request) can never exceed the quantity originally sold on that line.
    /// </summary>
    public static void EnsureExchangeQuantityAllowed(int alreadyConsumed, int requestedQuantity, int originallySold)
    {
        if (requestedQuantity <= 0)
        {
            throw new BusinessRuleException("Returned quantity must be greater than zero.");
        }

        if (alreadyConsumed + requestedQuantity > originallySold)
        {
            throw new BusinessRuleException(
                $"Cannot process {requestedQuantity} unit(s): {alreadyConsumed} already returned/exchanged out of {originallySold} sold.");
        }
    }

    /// <summary>
    /// Recalculates the invoice-level discount amount against a new subtotal after a
    /// return or exchange. Unlike CalculateDiscountAmount (used when an invoice is
    /// first created), this never throws: a percentage discount naturally scales
    /// with the new subtotal (up or down), and a fixed discount is capped at the new
    /// subtotal so the total still can't go negative (BR-11), instead of rejecting an
    /// otherwise-valid return/exchange.
    /// </summary>
    public static decimal RecalculateDiscountAfterAdjustment(decimal newSubtotal, string? discountType, decimal? discountValue)
    {
        if (discountType is null)
        {
            return 0m;
        }

        var value = discountValue ?? 0m;

        return discountType switch
        {
            "percentage" => newSubtotal * value / 100m,
            _ => Math.Min(value, Math.Max(newSubtotal, 0m)) // "fixed" (and any other stored value, defensively)
        };
    }

    /// <summary>BR-11: total = subtotal - discountAmount, and can never be negative.</summary>
    public static decimal CalculateTotal(decimal subtotal, decimal discountAmount)
    {
        var total = subtotal - discountAmount;

        if (total < 0)
        {
            throw new BusinessRuleException("Invoice total cannot be negative.");
        }

        return total;
    }
}