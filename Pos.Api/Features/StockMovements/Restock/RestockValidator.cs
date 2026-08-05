using FluentValidation;

namespace Pos.Api.Features.StockMovements.Restock
{
    public class RestockValidator : AbstractValidator<RestockCommand>
    {
        public RestockValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("معرف الصنف غير صالح");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
        }
    }
}