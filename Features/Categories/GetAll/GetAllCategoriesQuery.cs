using MediatR;

namespace Pos.Api.Features.Categories.GetAll
{
    public class GetAllCategoriesQuery : IRequest<List<CategoryDto>>
    {
        // فلترة اختيارية: هل بدك بس الفعّالة ولا الكل
        public bool OnlyActive { get; set; } = false;
    }
}