using POINTOFSALEBACKEND.Enums;

namespace POINTOFSALEBACKEND.Models
{
    public class StockMovement : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public StockMovementType Type { get; set; }

        public int QuantityInPieces { get; set; }

        public int BalanceBefore { get; set; }
        public int BalanceAfter { get; set; }

        public string? Reason { get; set; }

        
        public int? ReferenceInvoiceId { get; set; }

        
    }
}
