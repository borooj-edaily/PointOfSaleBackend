using Pos.Api.Enums;

namespace Pos.Api.Features.Products.GetAll
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public SellByType SellBy { get; set; }
        public int? PiecesPerPackage { get; set; }
        public decimal? PricePerPiece { get; set; }
        public decimal? PricePerPackage { get; set; }
        public int StockInPieces { get; set; }
        public bool IsActive { get; set; }
    }
}