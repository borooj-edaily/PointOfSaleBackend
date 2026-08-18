using Dapper;
using MediatR;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Customers.GetAll
{
    public class GetAllCustomersHandler
        : IRequestHandler<GetAllCustomersQuery, List<CustomerDto>>
    {
        private readonly IPosDatabase _database;

        public GetAllCustomersHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<List<CustomerDto>> Handle(
            GetAllCustomersQuery request,
            CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (request.OnlyActive)
            {
                whereClauses.Add("c.IsActive = TRUE");
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                whereClauses.Add(
                    "(c.Name LIKE @Search OR c.Phone LIKE @Search)"
                );

                parameters.Add(
                    "Search",
                    $"%{request.Search.Trim()}%"
                );
            }

            string whereSql =
                whereClauses.Count > 0
                    ? "WHERE " + string.Join(" AND ", whereClauses)
                    : "";

            var sql = $@"
                SELECT
                    c.Id,
                    c.Name,
                    c.Phone,
                    c.Notes,
                    c.IsActive,

                    IFNULL((
                        SELECT COUNT(*)
                        FROM Invoices i
                        WHERE i.CustomerId = c.Id
                    ), 0) AS InvoiceCount,

                    IFNULL((
                        SELECT SUM(i.Total)
                        FROM Invoices i
                        WHERE i.CustomerId = c.Id
                    ), 0) AS TotalPurchases,

                    IFNULL((
                        SELECT SUM(i.Total)
                        FROM Invoices i
                        WHERE i.CustomerId = c.Id
                          AND i.IsDebt = 1
                          AND i.DebtPaidAt IS NULL
                    ), 0) AS OutstandingDebt

                FROM Customers c

                {whereSql}

                ORDER BY
                    InvoiceCount DESC,
                    c.Name ASC;";

            var result =
                await connection.QueryAsync<CustomerDto>(
                    sql,
                    parameters
                );

            return result.ToList();
        }
    }
}