using FluentValidation;

namespace Pos.Api.Features.StockMovements.Deduct
{
    public class DeductStockValidator : AbstractValidator<DeductStockCommand>
    {
        public DeductStockValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("معرف الصنف غير صالح");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("سبب الخصم إلزامي")
                .MaximumLength(250).WithMessage("السبب يجب ألا يتجاوز 250 حرف");
        }
    }
}
