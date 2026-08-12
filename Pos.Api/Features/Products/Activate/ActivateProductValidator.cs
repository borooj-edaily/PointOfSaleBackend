using FluentValidation;

namespace Pos.Api.Features.Products.Activate
{
    public class ActivateProductValidator : AbstractValidator<ActivateProductCommand>
    {
        public ActivateProductValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("معرف الصنف غير صالح");
        }
    }
}