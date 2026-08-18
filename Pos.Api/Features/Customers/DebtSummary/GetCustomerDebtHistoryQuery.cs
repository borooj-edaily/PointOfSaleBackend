using MediatR;

namespace Pos.Api.Features.Customers.DebtSummary
{
    public class CustomerDebtInvoiceDto
    {
        public int InvoiceId { get; set; }

        public int InvoiceNumber { get; set; }

        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsDebt { get; set; }

        public DateTime? DebtPaidAt { get; set; }

        public bool IsPaid =>
            !IsDebt || DebtPaidAt.HasValue;
    }

    public class GetCustomerDebtHistoryResponse
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public decimal OutstandingDebt { get; set; }

        public List<CustomerDebtInvoiceDto> Invoices { get; set; } = new();
    }

    public class GetCustomerDebtHistoryQuery
        : IRequest<GetCustomerDebtHistoryResponse>
    {
        public int CustomerId { get; set; }
    }
}