using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Categories.Update
{
    public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Unit>
    {
        private readonly IPosDatabase _database;

        public UpdateCategoryHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<Unit> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string checkExistsSql = "SELECT COUNT(1) FROM Categories WHERE Id = @Id;";
            var exists = await connection.ExecuteScalarAsync<int>(checkExistsSql, new { request.Id });

            if (exists == 0)
                throw new NotFoundException($"لا يوجد كاتيجوري بالمعرف {request.Id}");

            const string checkNameSql = "SELECT COUNT(1) FROM Categories WHERE Name = @Name AND Id <> @Id;";
            var nameExists = await connection.ExecuteScalarAsync<int>(
                checkNameSql, new { request.Name, request.Id });

            if (nameExists > 0)
                throw new DuplicateResourceException($"يوجد كاتيجوري بنفس الاسم '{request.Name}' مسبقاً.");

            const string updateSql = @"
                UPDATE Categories
                SET Name = @Name,
                    UpdatedAt = UTC_TIMESTAMP(6),
                    UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id;";

            await connection.ExecuteAsync(updateSql, new
            {
                request.Id,
                request.Name,
                request.UpdatedByUserId
            });

            return Unit.Value;
        }
    }
}