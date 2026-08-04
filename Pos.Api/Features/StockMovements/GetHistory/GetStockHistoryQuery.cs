using MediatR;

namespace Pos.Api.Features.StockMovements.GetHistory
{
    public class GetStockHistoryQuery : IRequest<List<StockMovementDto>>
    {
        public int ProductId { get; set; }
    }
}