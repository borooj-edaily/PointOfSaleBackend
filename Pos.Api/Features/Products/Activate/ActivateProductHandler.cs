using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Products.Activate
{
    public class ActivateProductHandler : IRequestHandler<ActivateProductCommand, Unit>
    {
        private readonly IPosDatabase _database;

        public ActivateProductHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<Unit> Handle(ActivateProductCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string checkSql = @"
                SELECT CategoryId FROM Products WHERE Id = @Id;";

            var categoryId = await connection.QueryFirstOrDefaultAsync<int?>(checkSql, new { request.Id });

            if (categoryId is null)
                throw new NotFoundException($"لا يوجد صنف بالمعرف {request.Id}");

            using var transaction = connection.BeginTransaction();
            try
            {
                // 1) فعّل الصنف نفسه
                const string updateProductSql = @"
                    UPDATE Products
                    SET IsActive = TRUE,
                        UpdatedAt = UTC_TIMESTAMP(6),
                        UpdatedByUserId = @UpdatedByUserId
                    WHERE Id = @Id;";

                await connection.ExecuteAsync(updateProductSql,
                    new { request.Id, request.UpdatedByUserId }, transaction);

                // 2) لو الكاتيجوري تبعت الصنف معطّلة، فعّلها هي كمان تلقائياً
                const string activateCategorySql = @"
                    UPDATE Categories
                    SET IsActive = TRUE,
                        UpdatedAt = UTC_TIMESTAMP(6),
                        UpdatedByUserId = @UpdatedByUserId
                    WHERE Id = @CategoryId AND IsActive = FALSE;";

                await connection.ExecuteAsync(activateCategorySql,
                    new { CategoryId = categoryId, request.UpdatedByUserId }, transaction);

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