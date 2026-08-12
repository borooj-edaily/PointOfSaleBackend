using MediatR;

namespace Pos.Api.Features.Products.Activate
{
    public class ActivateProductCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}
