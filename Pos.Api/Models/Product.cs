using POINTOFSALEBACKEND.Enums;

namespace POINTOFSALEBACKEND.Models
{
    public class Product : AuditableEntity
    {
        public string Name { get; set; } = null!;
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public SellByType SellBy { get; set; }

        public int? PiecesPerPackage { get; set; }

        public decimal? PricePerPiece { get; set; }

        public decimal? PricePerPackage { get; set; }

        public int StockInPieces { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}
