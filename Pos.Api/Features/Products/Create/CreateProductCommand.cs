using MediatR;
using Pos.Api.Enums;

namespace Pos.Api.Features.Products.Create
{
    public class CreateProductCommand : IRequest<int>
    {
        public string Name { get; set; } = null!;
        public int CategoryId { get; set; }
        public SellByType SellBy { get; set; }
        public int? PiecesPerPackage { get; set; }
        public decimal? PricePerPiece { get; set; }
        public decimal? PricePerPackage { get; set; }
        public int StockInPieces { get; set; } = 0;

        public int? CreatedByUserId { get; set; }
    }
}