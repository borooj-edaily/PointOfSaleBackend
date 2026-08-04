using MediatR;
using Pos.Api.Features.Categories.GetAll;

namespace Pos.Api.Features.Categories.GetById
{
    public class GetCategoryByIdQuery : IRequest<CategoryDto?>
    {
        public int Id { get; set; }
    }
}