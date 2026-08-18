using MediatR;

namespace Pos.Api.Features.Customers.Deactivate
{
    public class DeactivateCustomerCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}
