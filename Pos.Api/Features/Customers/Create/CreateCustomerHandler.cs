using Dapper;
using MediatR;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Customers.Create
{
    public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, int>
    {
        private readonly IPosDatabase _database;

        public CreateCustomerHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<int> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            // ملاحظة: ما في تحقق من تكرار الاسم/التلفون قصدًا — ممكن يكون في
            // أكتر من زبون بنفس الاسم (الأب والابن مثلاً)، وهاد شي طبيعي
            // بدفتر الديون الورقي أصلاً.
            const string insertSql = @"
                INSERT INTO Customers
                    (Name, Phone, Notes, IsActive, CreatedAt, CreatedByUserId)
                VALUES
                    (@Name, @Phone, @Notes, TRUE, UTC_TIMESTAMP(6), @CreatedByUserId);
                SELECT LAST_INSERT_ID();";

            var newId = await connection.ExecuteScalarAsync<int>(insertSql, new
            {
                request.Name,
                request.Phone,
                request.Notes,
                request.CreatedByUserId
            });

            return newId;
        }
    }
}
