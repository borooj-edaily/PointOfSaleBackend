using Dapper;
using MediatR;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Products.LowStock
{
    public class GetLowStockProductsHandler : IRequestHandler<GetLowStockProductsQuery, List<LowStockProductDto>>
    {
        private readonly IPosDatabase _database;

        public GetLowStockProductsHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<List<LowStockProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            var sql = @"
                SELECT p.Id, p.Name, c.Name AS CategoryName, p.StockInPieces
                FROM Products p
                JOIN Categories c ON p.CategoryId = c.Id
                WHERE p.IsActive = TRUE";

            var parameters = new DynamicParameters();

            if (request.OnlyOutOfStock)
            {
                sql += " AND p.StockInPieces = 0";
            }
            else
            {
                sql += " AND p.StockInPieces <= @Threshold";
                parameters.Add("Threshold", request.Threshold);
            }

            sql += " ORDER BY p.StockInPieces ASC;";

            var result = await connection.QueryAsync<LowStockProductDto>(sql, parameters);
            return result.ToList();
        }
    }
}