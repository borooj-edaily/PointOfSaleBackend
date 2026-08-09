using MediatR;

namespace Pos.Api.Features.Categories.Update
{
    public class UpdateCategoryCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        // بيتحدد من الـ Controller (من التوكن/الـ User الحالي)، مش من الـ Frontend
        public int? UpdatedByUserId { get; set; }
    }
}