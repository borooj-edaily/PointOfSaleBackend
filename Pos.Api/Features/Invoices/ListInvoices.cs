using Dapper;
using MediatR;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Invoices;

/// <summary>
/// One row in the invoice history list (header only — no line items, to keep the
/// list light). Use GetInvoiceByNumber to drill into a single invoice's items.
/// </summary>
public class InvoiceListItemDto
{
    public int InvoiceId { get; set; }
    public int InvoiceNumber { get; set; }
    public int CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public bool HasReturn { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDebt { get; set; }
    public string? DebtorNickname { get; set; }
    public DateTime? DebtPaidAt { get; set; }
}

public class ListInvoicesResponse
{
    public List<InvoiceListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Invoice history query.
///
/// - RequestingUserId / RequestingUserCanViewAll are filled in by the controller from
///   the current user's claims, never from client input, so a cashier can never widen
///   the query to someone else's invoices just by tweaking the request body.
/// - CashierId is an optional extra filter, only honored when the requester is allowed
///   to view all invoices (e.g. an admin narrowing the list down to one cashier).
/// </summary>
public class ListInvoicesQuery : IRequest<ListInvoicesResponse>
{
    public int RequestingUserId { get; set; }
    public bool RequestingUserCanViewAll { get; set; }

    public int? CashierId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ListInvoicesHandler : IRequestHandler<ListInvoicesQuery, ListInvoicesResponse>
{
    private readonly IPosDatabase _database;

    public ListInvoicesHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<ListInvoicesResponse> Handle(ListInvoicesQuery request, CancellationToken ct)
    {
        using var connection = _database.Open();

        // A cashier without view_all_invoices can only ever see their own invoices,
        // regardless of what CashierId was requested (issue #5: "الكاشير يشوف فواتيره
        // او الادمن يشوف كل الفواتير").
        int? effectiveCashierId = request.RequestingUserCanViewAll
            ? request.CashierId
            : request.RequestingUserId;

        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;
        int offset = (page - 1) * pageSize;

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (effectiveCashierId.HasValue)
        {
            whereClauses.Add("i.CashierId = @CashierId");
            parameters.Add("CashierId", effectiveCashierId.Value);
        }

        if (request.FromDate.HasValue)
        {
            whereClauses.Add("i.CreatedAt >= @FromDate");
            parameters.Add("FromDate", request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            whereClauses.Add("i.CreatedAt < @ToDate");
            parameters.Add("ToDate", request.ToDate.Value);
        }

        string whereSql = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : string.Empty;

        var totalCount = await connection.QuerySingleAsync<int>(
            $"SELECT COUNT(*) FROM Invoices i {whereSql};",
            parameters);

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var items = await connection.QueryAsync<InvoiceListItemDto>(
            $"""
            SELECT
                i.Id AS InvoiceId,
                i.InvoiceNumber,
                i.CashierId,
                u.FullName AS CashierName,
                i.HasReturn,
                i.Subtotal,
                i.Total,
                i.CreatedAt,
                i.IsDebt,
                i.DebtorNickname,
                i.DebtPaidAt
            FROM Invoices i
            LEFT JOIN Users u ON u.Id = i.CashierId
            {whereSql}
            ORDER BY i.CreatedAt DESC, i.Id DESC
            LIMIT @PageSize OFFSET @Offset;
            """,
            parameters);

        return new ListInvoicesResponse
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}