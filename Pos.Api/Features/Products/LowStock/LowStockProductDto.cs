namespace Pos.Api.Features.Products.LowStock
{
    public class LowStockProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public int StockInPieces { get; set; }
    }
}