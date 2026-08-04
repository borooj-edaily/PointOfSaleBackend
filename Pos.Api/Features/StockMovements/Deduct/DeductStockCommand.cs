using MediatR;

namespace Pos.Api.Features.StockMovements.Deduct
{
    public class DeductStockCommand : IRequest<int>
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public bool IsPackage { get; set; }
        public string Reason { get; set; } = null!; // إلزامي (BR-17)
        public int? CreatedByUserId { get; set; }
    }
}