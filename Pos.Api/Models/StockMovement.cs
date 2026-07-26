using Pos.Api.Enums;

namespace Pos.Api.Models
{
    public class StockMovement : ImmutableEntity
    {
        public int ProductId { get; set; }
        public StockMovementType Type { get; set; }
        public int QuantityInPieces { get; set; }
        public int BalanceBefore { get; set; }
        public int BalanceAfter { get; set; }
        public string? Reason { get; set; }
        public int? ReferenceInvoiceId { get; set; }
    }
}