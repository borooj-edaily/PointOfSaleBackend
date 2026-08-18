using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Invoices;

// ---------------------------------------------------------------------------
// List debts ("مين متداين؟")
// ---------------------------------------------------------------------------

public class DebtListItemDto
{
    public int InvoiceId { get; set; }
    public int InvoiceNumber { get; set; }
    public string DebtorNickname { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerPhone { get; set; }
    public int CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DebtPaidAt { get; set; }
    public bool IsPaid => DebtPaidAt.HasValue;
}

public class ListDebtsResponse
{
    public List<DebtListItemDto> Items { get; set; } = new();
    public decimal TotalOutstanding { get; set; }
}

/// <summary>Any authenticated user with record_debt can see the full debt notebook —
/// this is deliberately not scoped per-cashier the way invoice history is, since
/// debt collection is usually a shared, store-wide concern.</summary>
public class ListDebtsQuery : IRequest<ListDebtsResponse>
{
    public bool OnlyUnpaid { get; set; } = true;
    public string? Nickname { get; set; }
    public int? CustomerId { get; set; }
}

public class ListDebtsHandler : IRequestHandler<ListDebtsQuery, ListDebtsResponse>
{
    private readonly IPosDatabase _database;

    public ListDebtsHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<ListDebtsResponse> Handle(ListDebtsQuery request, CancellationToken ct)
    {
        using var connection = _database.Open();

        var whereClauses = new List<string> { "i.IsDebt = 1" };
        var parameters = new DynamicParameters();

        if (request.OnlyUnpaid)
        {
            whereClauses.Add("i.DebtPaidAt IS NULL");
        }

        if (!string.IsNullOrWhiteSpace(request.Nickname))
        {
            // "Nickname" search also matches a linked customer's real name,
            // so the same search box works whether the debt was recorded
            // against a full customer file or a quick free-text nickname.
            whereClauses.Add("(i.DebtorNickname LIKE @Nickname OR c.Name LIKE @Nickname)");
            parameters.Add("Nickname", $"%{request.Nickname.Trim()}%");
        }

        if (request.CustomerId.HasValue)
        {
            whereClauses.Add("i.CustomerId = @CustomerId");
            parameters.Add("CustomerId", request.CustomerId.Value);
        }

        string whereSql = "WHERE " + string.Join(" AND ", whereClauses);

        var items = (await connection.QueryAsync<DebtListItemDto>(
            $"""
            SELECT
                i.Id AS InvoiceId,
                i.InvoiceNumber,
                i.DebtorNickname,
                i.CustomerId,
                c.Phone AS CustomerPhone,
                i.CashierId,
                u.FullName AS CashierName,
                i.Total,
                i.CreatedAt,
                i.DebtPaidAt
            FROM Invoices i
            LEFT JOIN Users u ON u.Id = i.CashierId
            LEFT JOIN Customers c ON c.Id = i.CustomerId
            {whereSql}
            ORDER BY i.DebtPaidAt IS NOT NULL, i.CreatedAt DESC;
            """,
            parameters)).ToList();

        return new ListDebtsResponse
        {
            Items = items,
            TotalOutstanding = items.Where(i => !i.IsPaid).Sum(i => i.Total)
        };
    }
}

// ---------------------------------------------------------------------------
// Mark a debt as paid ("تسديد الدين")
// ---------------------------------------------------------------------------

public class MarkDebtPaidCommand : IRequest<MarkDebtPaidResponse>
{
    public int InvoiceNumber { get; set; }
}

public class MarkDebtPaidResponse
{
    public int InvoiceNumber { get; set; }
    public DateTime DebtPaidAt { get; set; }
}

public class MarkDebtPaidHandler : IRequestHandler<MarkDebtPaidCommand, MarkDebtPaidResponse>
{
    private readonly IPosDatabase _database;

    public MarkDebtPaidHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<MarkDebtPaidResponse> Handle(MarkDebtPaidCommand request, CancellationToken ct)
    {
        using var connection = _database.Open();

        var invoice = await connection.QuerySingleOrDefaultAsync<(int Id, bool IsDebt, DateTime? DebtPaidAt)>(
            "SELECT Id, IsDebt, DebtPaidAt FROM Invoices WHERE InvoiceNumber = @InvoiceNumber",
            new { request.InvoiceNumber });

        if (invoice.Id == 0)
        {
            throw new NotFoundException($"Invoice {request.InvoiceNumber} not found.");
        }

        if (!invoice.IsDebt)
        {
            throw new BusinessRuleException($"Invoice {request.InvoiceNumber} was not recorded as a debt.");
        }

        if (invoice.DebtPaidAt.HasValue)
        {
            throw new BusinessRuleException($"Invoice {request.InvoiceNumber} is already marked as paid.");
        }

        var paidAt = DateTime.UtcNow;

        await connection.ExecuteAsync(
            "UPDATE Invoices SET DebtPaidAt = @PaidAt WHERE Id = @Id",
            new { PaidAt = paidAt, invoice.Id });

        return new MarkDebtPaidResponse
        {
            InvoiceNumber = request.InvoiceNumber,
            DebtPaidAt = paidAt
        };
    }
}