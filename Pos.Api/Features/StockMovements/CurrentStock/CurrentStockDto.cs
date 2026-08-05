namespace Pos.Api.Features.StockMovements.CurrentStock
{
    public class CurrentStockDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int StockInPieces { get; set; }
        public bool IsActive { get; set; }
    }
}