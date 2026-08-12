using FluentValidation;

namespace Pos.Api.Features.Categories.Activate
{
    public class ActivateCategoryValidator : AbstractValidator<ActivateCategoryCommand>
    {
        public ActivateCategoryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("معرف الكاتيجوري غير صالح");
        }
    }
}