using FluentValidation;

namespace Pos.Api.Features.Customers.Deactivate
{
    public class DeactivateCustomerValidator : AbstractValidator<DeactivateCustomerCommand>
    {
        public DeactivateCustomerValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("معرف الزبون غير صالح");
        }
    }
}
