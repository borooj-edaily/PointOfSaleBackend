using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Categories.Activate
{
    public class ActivateCategoryHandler : IRequestHandler<ActivateCategoryCommand, Unit>
    {
        private readonly IPosDatabase _database;

        public ActivateCategoryHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<Unit> Handle(ActivateCategoryCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string checkSql = "SELECT COUNT(1) FROM Categories WHERE Id = @Id;";
            var exists = await connection.ExecuteScalarAsync<int>(checkSql, new { request.Id });

            if (exists == 0)
                throw new NotFoundException($"لا يوجد كاتيجوري بالمعرف {request.Id}");

            const string updateSql = @"
                UPDATE Categories
                SET IsActive = TRUE,
                    UpdatedAt = UTC_TIMESTAMP(6),
                    UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id;";

            await connection.ExecuteAsync(updateSql, new { request.Id, request.UpdatedByUserId });

            return Unit.Value;
        }
    }
}