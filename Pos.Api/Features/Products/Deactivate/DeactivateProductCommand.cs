using MediatR;

namespace Pos.Api.Features.Products.Deactivate
{
    public class DeactivateProductCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}