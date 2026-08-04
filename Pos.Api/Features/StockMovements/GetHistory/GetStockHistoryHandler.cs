using Dapper;
using MediatR;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.StockMovements.GetHistory
{
    public class GetStockHistoryHandler : IRequestHandler<GetStockHistoryQuery, List<StockMovementDto>>
    {
        private readonly IPosDatabase _database;

        public GetStockHistoryHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<List<StockMovementDto>> Handle(GetStockHistoryQuery request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string sql = @"
                SELECT Id, ProductId, Type, QuantityInPieces, BalanceBefore, BalanceAfter,
                       Reason, ReferenceInvoiceId, CreatedAt, CreatedByUserId
                FROM StockMovements
                WHERE ProductId = @ProductId
                ORDER BY CreatedAt DESC;";

            var result = await connection.QueryAsync<StockMovementDto>(sql, new { request.ProductId });
            return result.ToList();
        }
    }
}