namespace Pos.Api.Features.Dashboard.GetStats
{
    public class DashboardStatsDto
    {
        public decimal TodaySalesTotal { get; set; }
        public int TodayInvoicesCount { get; set; }
        public decimal MonthSalesTotal { get; set; }
        public int TotalInvoicesCount { get; set; }
    }
}