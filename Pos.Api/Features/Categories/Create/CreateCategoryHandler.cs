using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Categories.Create
{
    public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, int>
    {
        private readonly IPosDatabase _database;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public CreateCategoryHandler(IPosDatabase database)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            _database = database;
        }

        public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string checkExistsSql = "SELECT COUNT(1) FROM Categories WHERE Name = @Name;";
            var exists = await connection.ExecuteScalarAsync<int>(checkExistsSql, new { request.Name });

            if (exists > 0)
                throw new DuplicateResourceException($"يوجد كاتيجوري بنفس الاسم '{request.Name}' مسبقاً.");

            const string insertSql = @"
                INSERT INTO Categories (Name, IsActive, CreatedAt, CreatedByUserId)
                VALUES (@Name, TRUE, UTC_TIMESTAMP(6), @CreatedByUserId);
                SELECT LAST_INSERT_ID();";

            var newId = await connection.ExecuteScalarAsync<int>(insertSql, new
            {
                request.Name,
                request.CreatedByUserId
            });

            return newId;
        }
    }
}