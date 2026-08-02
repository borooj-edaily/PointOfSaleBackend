using Dapper;
using MediatR;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Dashboard.GetStats
{
    public class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IPosDatabase _database;

        public GetDashboardStatsHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            var sql = @"
                SELECT
                    (SELECT COALESCE(SUM(Total), 0) FROM Invoices WHERE DATE(CreatedAt) = CURDATE()) AS TodaySalesTotal,
                    (SELECT COUNT(*) FROM Invoices WHERE DATE(CreatedAt) = CURDATE()) AS TodayInvoicesCount,
                    (SELECT COALESCE(SUM(Total), 0) FROM Invoices WHERE YEAR(CreatedAt) = YEAR(CURDATE()) AND MONTH(CreatedAt) = MONTH(CURDATE())) AS MonthSalesTotal,
                    (SELECT COUNT(*) FROM Invoices) AS TotalInvoicesCount;";

            var result = await connection.QuerySingleAsync<DashboardStatsDto>(sql);
            return result;
        }
    }
}