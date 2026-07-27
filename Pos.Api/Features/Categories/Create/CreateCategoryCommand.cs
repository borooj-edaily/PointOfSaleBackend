using MediatR;

namespace Pos.Api.Features.Categories.Create
{
    public class CreateCategoryCommand : IRequest<int>
    {
        public string Name { get; set; } = null!;

        // بيتحدد من الـ Controller (من التوكن/الـ User الحالي)، مش من الـ Frontend
        public int? CreatedByUserId { get; set; }
    }
}