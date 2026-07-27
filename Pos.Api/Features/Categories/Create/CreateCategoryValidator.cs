using FluentValidation;

namespace Pos.Api.Features.Categories.Create
{
    public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("اسم الكاتيجوري مطلوب")
                .MaximumLength(100).WithMessage("اسم الكاتيجوري يجب ألا يتجاوز 100 حرف");
        }
    }
}