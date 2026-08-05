using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.StockMovements.CurrentStock
{
    public class GetCurrentStockHandler : IRequestHandler<GetCurrentStockQuery, CurrentStockDto>
    {
        private readonly IPosDatabase _database;

        public GetCurrentStockHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<CurrentStockDto> Handle(GetCurrentStockQuery request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string sql = @"
                SELECT Id AS ProductId, Name AS ProductName, StockInPieces, IsActive
                FROM Products
                WHERE Id = @ProductId;";

            var result = await connection.QueryFirstOrDefaultAsync<CurrentStockDto>(
                sql, new { request.ProductId });

            if (result is null)
                throw new NotFoundException($"لا يوجد صنف بالمعرف {request.ProductId}");

            return result;
        }
    }
}