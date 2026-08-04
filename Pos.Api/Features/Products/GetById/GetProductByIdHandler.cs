using Dapper;
using MediatR;
using Pos.Api.Features.Products.GetAll;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Products.GetById
{
    public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
    {
        private readonly IPosDatabase _database;

        public GetProductByIdHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string sql = @"
                SELECT
                    p.Id, p.Name, p.CategoryId,
                    c.Name AS CategoryName,
                    p.SellBy, p.PiecesPerPackage, p.PricePerPiece, p.PricePerPackage,
                    p.StockInPieces, p.IsActive
                FROM Products p
                JOIN Categories c ON p.CategoryId = c.Id
                WHERE p.Id = @Id;";

            var result = await connection.QueryFirstOrDefaultAsync<ProductDto>(sql, new { request.Id });
            return result;
        }
    }
}