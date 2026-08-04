using MediatR;

namespace Pos.Api.Features.Products.GetAll
{
    public class GetAllProductsQuery : IRequest<List<ProductDto>>
    {
        public int? CategoryId { get; set; }
        public bool OnlyActive { get; set; } = false;
        public string? SearchTerm { get; set; }
    }
}