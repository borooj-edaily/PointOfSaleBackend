using MediatR;

namespace Pos.Api.Features.Customers.Update
{
    public class UpdateCustomerCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Notes { get; set; }

        public int? UpdatedByUserId { get; set; }
    }
}
