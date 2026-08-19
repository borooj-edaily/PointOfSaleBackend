using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Categories.Deactivate
{
    public class DeactivateCategoryHandler : IRequestHandler<DeactivateCategoryCommand, Unit>
    {
        private readonly IPosDatabase _database;

        public DeactivateCategoryHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<Unit> Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string checkSql = "SELECT COUNT(1) FROM Categories WHERE Id = @Id;";
            var exists = await connection.ExecuteScalarAsync<int>(checkSql, new { request.Id });

            if (exists == 0)
                throw new NotFoundException($"لا يوجد كاتيجوري بالمعرف {request.Id}");

            using var transaction = connection.BeginTransaction();
            try
            {
                // 1) عطّل الكاتيجوري نفسها
                const string deactivateCategorySql = @"
                    UPDATE Categories
                    SET IsActive = FALSE,
                        UpdatedAt = UTC_TIMESTAMP(6),
                        UpdatedByUserId = @UpdatedByUserId
                    WHERE Id = @Id;";

                await connection.ExecuteAsync(deactivateCategorySql,
                    new { request.Id, request.UpdatedByUserId }, transaction);

                // 2) عطّل كل الأصناف الفعّالة يلي جوا هاي الكاتيجوري (Cascade)
                const string deactivateProductsSql = @"
                    UPDATE Products
                    SET IsActive = FALSE,
                        UpdatedAt = UTC_TIMESTAMP(6),
                        UpdatedByUserId = @UpdatedByUserId
                    WHERE CategoryId = @Id AND IsActive = TRUE;";

                await connection.ExecuteAsync(deactivateProductsSql,
                    new { request.Id, request.UpdatedByUserId }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return Unit.Value;
        }
    }
}