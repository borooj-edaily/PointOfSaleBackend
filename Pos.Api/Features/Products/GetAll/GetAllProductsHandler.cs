using Dapper;
using MediatR;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Products.GetAll
{
    public class GetAllProductsHandler : IRequestHandler<GetAllProductsQuery, List<ProductDto>>
    {
        private readonly IPosDatabase _database;

        public GetAllProductsHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<List<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            var sql = @"
                SELECT
                    p.Id, p.Name, p.CategoryId,
                    c.Name AS CategoryName,
                    p.SellBy, p.PiecesPerPackage, p.PricePerPiece, p.PricePerPackage,
                    p.StockInPieces, p.IsActive
                FROM Products p
                JOIN Categories c ON p.CategoryId = c.Id
                WHERE 1 = 1";

            var parameters = new DynamicParameters();

            if (request.CategoryId.HasValue)
            {
                sql += " AND p.CategoryId = @CategoryId";
                parameters.Add("CategoryId", request.CategoryId.Value);
            }

            if (request.OnlyActive)
            {
                sql += " AND p.IsActive = TRUE";
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                sql += " AND p.Name LIKE @SearchTerm";
                parameters.Add("SearchTerm", $"%{request.SearchTerm}%");
            }

            sql += " ORDER BY p.Name;";

            var result = await connection.QueryAsync<ProductDto>(sql, parameters);
            return result.ToList();
        }
    }
}