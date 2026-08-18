using FluentValidation;

namespace Pos.Api.Features.Customers.Update
{
    public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
    {
        public UpdateCustomerValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("معرف الزبون غير صالح");

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
