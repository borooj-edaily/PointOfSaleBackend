using FluentValidation;

namespace Pos.Api.Features.Customers.Create
{
    public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
    {
        public CreateCustomerValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("اسم الزبون مطلوب")
                .MaximumLength(150).WithMessage("اسم الزبون يجب ألا يتجاوز 150 حرف");

            RuleFor(x => x.Phone)
                .MaximumLength(30).WithMessage("رقم الهاتف يجب ألا يتجاوز 30 حرف");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("الملاحظات يجب ألا تتجاوز 500 حرف");
        }
    }
}
