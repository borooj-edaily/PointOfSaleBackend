using MediatR;
using Pos.Api.Features.Products.GetAll;

namespace Pos.Api.Features.Products.GetById
{
    public class GetProductByIdQuery : IRequest<ProductDto?>
    {
        public int Id { get; set; }
    }
}