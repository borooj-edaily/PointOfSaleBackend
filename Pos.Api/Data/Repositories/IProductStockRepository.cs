namespace Pos.Api.Data.Repositories;

public record ProductStockInfo(
    int ProductId,
    decimal PricePerPiece,
    decimal? PricePerPackage,
    int? PiecesPerPackage,
    int StockInPieces
);

/// <summary>
/// Abstraction over product stock lookups so the Invoices module can be built
/// and tested before Person B's real Products table/queries exist.
/// Swap PlaceholderProductStockRepository for a real Dapper-based
/// implementation (querying the Products table) during card 7 (final
/// integration) without touching the handler that depends on this interface.
/// </summary>
public interface IProductStockRepository
{
    Task<ProductStockInfo?> GetStockAsync(int productId);
    Task DecrementStockAsync(int productId, int quantityInPieces);
}

/// <summary>
/// TEMPORARY placeholder implementation used only until Person B's Products
/// table and real repository are ready. Remove this class in card 7.
/// </summary>
public class PlaceholderProductStockRepository : IProductStockRepository
{
    private static readonly Dictionary<int, ProductStockInfo> _fakeStock = new()
    {
        [1] = new ProductStockInfo(1, PricePerPiece: 2.5m, PricePerPackage: 55m, PiecesPerPackage: 24, StockInPieces: 100),
        [2] = new ProductStockInfo(2, PricePerPiece: 1.0m, PricePerPackage: null, PiecesPerPackage: null, StockInPieces: 50),
    };

    public Task<ProductStockInfo?> GetStockAsync(int productId)
    {
        _fakeStock.TryGetValue(productId, out var info);
        return Task.FromResult(info);
    }

    public Task DecrementStockAsync(int productId, int quantityInPieces)
    {
        if (_fakeStock.TryGetValue(productId, out var info))
        {
            _fakeStock[productId] = info with { StockInPieces = info.StockInPieces - quantityInPieces };
        }
        return Task.CompletedTask;
    }
}
