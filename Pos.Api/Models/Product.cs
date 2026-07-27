using Pos.Api.Enums;

namespace Pos.Api.Models
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = null!;
        public int CategoryId { get; set; }
        public SellByType SellBy { get; set; }
        public int? PiecesPerPackage { get; set; }
        public decimal? PricePerPiece { get; set; }
        public decimal? PricePerPackage { get; set; }
        public int StockInPieces { get; set; }
        public bool IsActive { get; set; } = true;
    }
}