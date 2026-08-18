using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Customers.DebtSummary
{
    public class GetCustomerDebtHistoryHandler
        : IRequestHandler<
            GetCustomerDebtHistoryQuery,
            GetCustomerDebtHistoryResponse>
    {
        private readonly IPosDatabase _database;

        public GetCustomerDebtHistoryHandler(
            IPosDatabase database)
        {
            _database = database;
        }

        public async Task<GetCustomerDebtHistoryResponse> Handle(
            GetCustomerDebtHistoryQuery request,
            CancellationToken ct)
        {
            using var connection = _database.Open();

            var customer =
                await connection.QuerySingleOrDefaultAsync<
                    (int Id, string Name, string? Phone)
                >(
                    """
                    SELECT
                        Id,
                        Name,
                        Phone
                    FROM Customers
                    WHERE Id = @CustomerId
                    """,
                    new
                    {
                        request.CustomerId
                    });

            if (customer.Id == 0)
            {
                throw new NotFoundException(
                    $"لا يوجد زبون بالمعرف {request.CustomerId}"
                );
            }

            // Get ALL invoices belonging to this customer.
            // Cash and Debt are both included.
            var invoices =
                (
                    await connection.QueryAsync<CustomerDebtInvoiceDto>(
                        """
                        SELECT
                            Id AS InvoiceId,
                            InvoiceNumber,
                            Total,
                            CreatedAt,
                            IsDebt,
                            DebtPaidAt
                        FROM Invoices
                        WHERE CustomerId = @CustomerId
                        ORDER BY CreatedAt DESC, Id DESC;
                        """,
                        new
                        {
                            request.CustomerId
                        }
                    )
                ).ToList();

            var outstandingDebt =
                invoices
                    .Where(i =>
                        i.IsDebt &&
                        !i.DebtPaidAt.HasValue)
                    .Sum(i => i.Total);

            return new GetCustomerDebtHistoryResponse
            {
                CustomerId = customer.Id,

                CustomerName = customer.Name,

                Phone = customer.Phone,

                OutstandingDebt = outstandingDebt,

                Invoices = invoices
            };
        }
    }
}