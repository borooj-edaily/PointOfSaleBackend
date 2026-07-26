using FluentValidation;

namespace Pos.Api.Features.Categories.Deactivate
{
    public class DeactivateCategoryValidator : AbstractValidator<DeactivateCategoryCommand>
    {
        public DeactivateCategoryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("معرف الكاتيجوري غير صالح");
        }
    }
}