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
                SELECT Id, Name, IsActive
                FROM Categories";

            if (request.OnlyActive)
                sql += " WHERE IsActive = TRUE";

            sql += " ORDER BY Name;";

            var result = await connection.QueryAsync<CategoryDto>(sql);
            return result.ToList();
        }
    }
}