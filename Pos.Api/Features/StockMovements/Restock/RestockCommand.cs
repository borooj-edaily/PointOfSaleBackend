using MediatR;

namespace Pos.Api.Features.StockMovements.Restock
{
    public class RestockCommand : IRequest<int>
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public bool IsPackage { get; set; } // true = العبوة، false = القطعة
        public int? CreatedByUserId { get; set; }
    }
}