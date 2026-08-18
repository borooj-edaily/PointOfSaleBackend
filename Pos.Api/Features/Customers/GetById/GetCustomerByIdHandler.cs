using Dapper;
using MediatR;
using Pos.Api.Features.Customers.GetAll;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Customers.GetById
{
    public class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
    {
        private readonly IPosDatabase _database;

        public GetCustomerByIdHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<CustomerDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string sql = @"
                SELECT
                    c.Id, c.Name, c.Phone, c.Notes, c.IsActive,
                    IFNULL((
                        SELECT SUM(i.Total)
                        FROM Invoices i
                        WHERE i.CustomerId = c.Id AND i.IsDebt = 1 AND i.DebtPaidAt IS NULL
                    ), 0) AS OutstandingDebt
                FROM Customers c
                WHERE c.Id = @Id;";

            var result = await connection.QueryFirstOrDefaultAsync<CustomerDto>(sql, new { request.Id });
            return result;
        }
    }
}
