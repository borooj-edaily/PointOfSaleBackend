using MediatR;
using Pos.Api.Features.Customers.GetAll;

namespace Pos.Api.Features.Customers.GetById
{
    public class GetCustomerByIdQuery : IRequest<CustomerDto?>
    {
        public int Id { get; set; }
    }
}
