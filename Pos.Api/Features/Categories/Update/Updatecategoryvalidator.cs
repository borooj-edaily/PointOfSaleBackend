using FluentValidation;

namespace Pos.Api.Features.Categories.Update
{
    public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("معرف الكاتيجوري مطلوب");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("اسم الكاتيجوري مطلوب")
                .MaximumLength(100).WithMessage("اسم الكاتيجوري يجب ألا يتجاوز 100 حرف");
        }
    }
}