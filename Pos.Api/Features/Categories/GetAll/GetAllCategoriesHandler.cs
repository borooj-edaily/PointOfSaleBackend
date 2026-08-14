using Dapper;
using MediatR;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Categories.GetAll
{
    public class GetAllCategoriesHandler : IRequestHandler<GetAllCategoriesQuery, List<CategoryDto>>
    {
        private readonly IPosDatabase _database;

        public GetAllCategoriesHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<List<CategoryDto>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            var sql = @"
                SELECT
                    c.Id, c.Name, c.IsActive,
                    COUNT(p.Id) AS ProductsCount
                FROM Categories c
                LEFT JOIN Products p ON p.CategoryId = c.Id";

            if (request.OnlyActive)
                sql += " WHERE c.IsActive = TRUE";

            sql += @"
                GROUP BY c.Id, c.Name, c.IsActive
                ORDER BY c.Name;";

            var result = await connection.QueryAsync<CategoryDto>(sql);
            return result.ToList();
        }
    }
}