using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Customers.Deactivate
{
    public class DeactivateCustomerHandler : IRequestHandler<DeactivateCustomerCommand, Unit>
    {
        private readonly IPosDatabase _database;

        public DeactivateCustomerHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<Unit> Handle(DeactivateCustomerCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string checkSql = "SELECT COUNT(1) FROM Customers WHERE Id = @Id;";
            var exists = await connection.ExecuteScalarAsync<int>(checkSql, new { request.Id });

            if (exists == 0)
                throw new NotFoundException($"لا يوجد زبون بالمعرف {request.Id}");

            // ما منسمح نعطّل زبون وهو لسا عليه دين مفتوح — أول لازم يتسدد
            // أو يترحّل، حتى ما يضيع من قائمة "مين متداين" بالغلط.
            const string outstandingDebtSql = @"
                SELECT COUNT(1) FROM Invoices
                WHERE CustomerId = @Id AND IsDebt = 1 AND DebtPaidAt IS NULL;";

            var hasOutstandingDebt = await connection.ExecuteScalarAsync<int>(outstandingDebtSql, new { request.Id });

            if (hasOutstandingDebt > 0)
                throw new BusinessRuleException("لا يمكن تعطيل زبون عليه دين غير مسدد بعد.");

            const string updateSql = @"
                UPDATE Customers
                SET IsActive = FALSE,
                    UpdatedAt = UTC_TIMESTAMP(6),
                    UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id;";

            await connection.ExecuteAsync(updateSql, new { request.Id, request.UpdatedByUserId });

            return Unit.Value;
        }
    }
}
