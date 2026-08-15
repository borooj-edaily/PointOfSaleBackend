using Dapper;
using MediatR;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Reports;

public class DailySalesPointDto
{
    public DateTime Date { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalSales { get; set; }
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class CashierSalesDto
{
    public int CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalSales { get; set; }
}

public class SalesReportResponse
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public int TotalInvoices { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal NetSales { get; set; }
    public decimal TotalReturnsValue { get; set; }
    public decimal AverageInvoiceValue { get; set; }

    public List<DailySalesPointDto> DailyBreakdown { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<CashierSalesDto> SalesByCashier { get; set; } = new();
}

/// <summary>
/// FromDate/ToDate default to the last 30 days (inclusive of today) when not supplied.
/// ToDate is treated as exclusive-of-the-next-day internally so "today" is fully included.
/// </summary>
public class SalesReportQuery : IRequest<SalesReportResponse>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class SalesReportHandler : IRequestHandler<SalesReportQuery, SalesReportResponse>
{
    private readonly IPosDatabase _database;

    public SalesReportHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<SalesReportResponse> Handle(SalesReportQuery request, CancellationToken ct)
    {
        var toDateExclusive = (request.ToDate?.Date ?? DateTime.UtcNow.Date).AddDays(1);
        var fromDate = (request.FromDate?.Date ?? toDateExclusive.AddDays(-30));

        using var connection = _database.Open();

        var parameters = new { FromDate = fromDate, ToDate = toDateExclusive };

        var summary = await connection.QuerySingleAsync<SummaryRow>(
            """
            SELECT
                COUNT(*)                                   AS TotalInvoices,
                COALESCE(SUM(Subtotal), 0)                 AS GrossSales,
                COALESCE(SUM(Subtotal - Total), 0)          AS TotalDiscount,
                COALESCE(SUM(Total), 0)                     AS NetSales
            FROM Invoices
            WHERE CreatedAt >= @FromDate AND CreatedAt < @ToDate;
            """,
            parameters);

        var totalReturnsValue = await connection.QuerySingleAsync<decimal>(
            """
            SELECT COALESCE(SUM(ir.ReturnedQuantity * ii.UnitPriceSnapshot), 0)
            FROM InvoiceReturns ir
            JOIN InvoiceItems ii ON ii.Id = ir.InvoiceItemId
            WHERE ir.CreatedAt >= @FromDate AND ir.CreatedAt < @ToDate;
            """,
            parameters);

        var dailyBreakdown = await connection.QueryAsync<DailySalesPointDto>(
            """
            SELECT
                DATE(CreatedAt) AS Date,
                COUNT(*)        AS InvoiceCount,
                COALESCE(SUM(Total), 0) AS TotalSales
            FROM Invoices
            WHERE CreatedAt >= @FromDate AND CreatedAt < @ToDate
            GROUP BY DATE(CreatedAt)
            ORDER BY DATE(CreatedAt);
            """,
            parameters);

        var topProducts = await connection.QueryAsync<TopProductDto>(
            """
            SELECT
                p.Id AS ProductId,
                p.Name AS ProductName,
                COALESCE(SUM(ii.Quantity), 0) AS QuantitySold,
                COALESCE(SUM(ii.LineTotal), 0) AS Revenue
            FROM InvoiceItems ii
            JOIN Invoices i ON i.Id = ii.InvoiceId
            JOIN Products p ON p.Id = ii.ProductId
            WHERE i.CreatedAt >= @FromDate AND i.CreatedAt < @ToDate
            GROUP BY p.Id, p.Name
            ORDER BY Revenue DESC
            LIMIT 10;
            """,
            parameters);

        var salesByCashier = await connection.QueryAsync<CashierSalesDto>(
            """
            SELECT
                u.Id AS CashierId,
                u.FullName AS CashierName,
                COUNT(i.Id) AS InvoiceCount,
                COALESCE(SUM(i.Total), 0) AS TotalSales
            FROM Invoices i
            JOIN Users u ON u.Id = i.CashierId
            WHERE i.CreatedAt >= @FromDate AND i.CreatedAt < @ToDate
            GROUP BY u.Id, u.FullName
            ORDER BY TotalSales DESC;
            """,
            parameters);

        return new SalesReportResponse
        {
            FromDate = fromDate,
            ToDate = toDateExclusive.AddDays(-1),
            TotalInvoices = summary.TotalInvoices,
            GrossSales = summary.GrossSales,
            TotalDiscount = summary.TotalDiscount,
            NetSales = summary.NetSales,
            TotalReturnsValue = totalReturnsValue,
            AverageInvoiceValue = summary.TotalInvoices == 0
                ? 0
                : Math.Round(summary.NetSales / summary.TotalInvoices, 2),
            DailyBreakdown = dailyBreakdown.ToList(),
            TopProducts = topProducts.ToList(),
            SalesByCashier = salesByCashier.ToList()
        };
    }

    private sealed class SummaryRow
    {
        public int TotalInvoices { get; set; }
        public decimal GrossSales { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal NetSales { get; set; }
    }
}