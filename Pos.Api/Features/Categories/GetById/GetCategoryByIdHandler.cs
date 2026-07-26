using Dapper;
using MediatR;
using Pos.Api.Features.Categories.GetAll;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Categories.GetById
{
    public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto?>
    {
        private readonly IPosDatabase _database;

        public GetCategoryByIdHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<CategoryDto?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string sql = @"
                SELECT Id, Name, IsActive
                FROM Categories
                WHERE Id = @Id;";

            var result = await connection.QueryFirstOrDefaultAsync<CategoryDto>(sql, new { request.Id });
            return result;
        }
    }
}