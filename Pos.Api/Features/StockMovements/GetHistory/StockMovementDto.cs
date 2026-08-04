using Pos.Api.Enums;

namespace Pos.Api.Features.StockMovements.GetHistory
{
    public class StockMovementDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public StockMovementType Type { get; set; }
        public int QuantityInPieces { get; set; }
        public int BalanceBefore { get; set; }
        public int BalanceAfter { get; set; }
        public string? Reason { get; set; }
        public int? ReferenceInvoiceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
    }
}