using FluentValidation;
using Pos.Api.Enums;

namespace Pos.Api.Features.Products.Update
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("معرف الصنف مطلوب");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("اسم الصنف مطلوب")
                .MaximumLength(150).WithMessage("اسم الصنف يجب ألا يتجاوز 150 حرف");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("يجب اختيار كاتيجوري صحيحة");

            RuleFor(x => x.SellBy)
                .IsInEnum().WithMessage("طريقة البيع غير صالحة");

            // -------- التحقق الشرطي حسب SellBy (BR-02.1)، نفس قاعدة الإنشاء --------

            RuleFor(x => x.PricePerPiece)
                .NotNull().WithMessage("سعر الحبة مطلوب عند البيع بالحبة")
                .GreaterThan(0).WithMessage("سعر الحبة يجب أن يكون أكبر من صفر")
                .When(x => x.SellBy == SellByType.Piece || x.SellBy == SellByType.Both);

            RuleFor(x => x.PricePerPackage)
                .NotNull().WithMessage("سعر الباكيج مطلوب عند البيع بالباكيج")
                .GreaterThan(0).WithMessage("سعر الباكيج يجب أن يكون أكبر من صفر")
                .When(x => x.SellBy == SellByType.Package || x.SellBy == SellByType.Both);

            RuleFor(x => x.PiecesPerPackage)
                .NotNull().WithMessage("عدد الحبات بالباكيج مطلوب عند البيع بالباكيج")
                .GreaterThan(0).WithMessage("عدد الحبات بالباكيج يجب أن يكون أكبر من صفر")
                .When(x => x.SellBy == SellByType.Package || x.SellBy == SellByType.Both);
        }
    }
}