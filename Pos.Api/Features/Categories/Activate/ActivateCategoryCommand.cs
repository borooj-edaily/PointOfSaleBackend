using MediatR;

namespace Pos.Api.Features.Categories.Activate
{
    public class ActivateCategoryCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}