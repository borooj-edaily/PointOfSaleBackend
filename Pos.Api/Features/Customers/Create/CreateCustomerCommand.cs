using MediatR;

namespace Pos.Api.Features.Customers.Create
{
    public class CreateCustomerCommand : IRequest<int>
    {
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Notes { get; set; }

        public int? CreatedByUserId { get; set; }
    }
}
