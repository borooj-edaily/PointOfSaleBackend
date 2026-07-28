using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Products.Deactivate
{
    public class DeactivateProductHandler : IRequestHandler<DeactivateProductCommand, Unit>
    {
        private readonly IPosDatabase _database;

        public DeactivateProductHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<Unit> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string checkSql = "SELECT COUNT(1) FROM Products WHERE Id = @Id;";
            var exists = await connection.ExecuteScalarAsync<int>(checkSql, new { request.Id });

            if (exists == 0)
                throw new NotFoundException($"لا يوجد صنف بالمعرف {request.Id}");

            // تعطيل بدل حذف (BR-05) — الصنف يضل موجود بالفواتير والتقارير القديمة
            // بس ما يظهر لبيع جديد بشاشة الكاشير
            const string updateSql = @"
                UPDATE Products
                SET IsActive = FALSE,
                    UpdatedAt = UTC_TIMESTAMP(6),
                    UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id;";

            await connection.ExecuteAsync(updateSql, new { request.Id, request.UpdatedByUserId });

            return Unit.Value;
        }
    }
}