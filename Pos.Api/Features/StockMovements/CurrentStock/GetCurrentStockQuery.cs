using MediatR;

namespace Pos.Api.Features.StockMovements.CurrentStock
{
    public class GetCurrentStockQuery : IRequest<CurrentStockDto>
    {
        public int ProductId { get; set; }
    }
}