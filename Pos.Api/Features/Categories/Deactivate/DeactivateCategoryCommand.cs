using MediatR;

namespace Pos.Api.Features.Categories.Deactivate
{
    public class DeactivateCategoryCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}