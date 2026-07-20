using FluentValidation;

namespace Pos.Api.Features.Invoices.Finalize;

public class FinalizeInvoiceValidator : AbstractValidator<FinalizeInvoiceCommand>
{
    public FinalizeInvoiceValidator()
    {
        RuleFor(x => x.CashierId)
            .GreaterThan(0).WithMessage("CashierId is required.");

        // BR-01: an invoice must never be empty
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Invoice must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");
            item.RuleFor(i => i.UnitSold)
                .Must(u => u == "piece" || u == "package")
                .WithMessage("UnitSold must be either 'piece' or 'package'.");
        });

        RuleFor(x => x.DiscountType)
            .Must(t => t == null || t == "fixed" || t == "percentage")
            .WithMessage("DiscountType must be 'fixed', 'percentage', or null.");

        When(x => x.DiscountType == "percentage" && x.DiscountValue.HasValue, () =>
        {
            RuleFor(x => x.DiscountValue!.Value)
                .InclusiveBetween(0, 100)
                .WithMessage("Percentage discount must be between 0 and 100.");
        });

        When(x => x.DiscountType != null, () =>
        {
            RuleFor(x => x.DiscountValue)
                .NotNull().WithMessage("DiscountValue is required when DiscountType is set.");
        });
    }
}
