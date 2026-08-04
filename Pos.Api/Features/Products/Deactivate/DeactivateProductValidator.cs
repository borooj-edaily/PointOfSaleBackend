using FluentValidation;

namespace Pos.Api.Features.Products.Deactivate
{
    public class DeactivateProductValidator : AbstractValidator<DeactivateProductCommand>
    {
        public DeactivateProductValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("معرف الصنف غير صالح");
        }
    }
}