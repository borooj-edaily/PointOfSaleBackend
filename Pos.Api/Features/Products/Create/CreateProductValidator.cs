using FluentValidation;
using Pos.Api.Enums;

namespace Pos.Api.Features.Products.Create
{
    public class CreateProductValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("اسم الصنف مطلوب")
                .MaximumLength(150).WithMessage("اسم الصنف يجب ألا يتجاوز 150 حرف");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("يجب اختيار كاتيجوري صحيحة");

            RuleFor(x => x.SellBy)
                .IsInEnum().WithMessage("طريقة البيع غير صالحة");

            RuleFor(x => x.StockInPieces)
                .GreaterThanOrEqualTo(0).WithMessage("المخزون لا يمكن أن يكون سالباً");

            // -------- التحقق الشرطي حسب SellBy (BR-02.1) --------

            // لو SellBy تتضمن Piece (Piece أو Both) → PricePerPiece إلزامي
            RuleFor(x => x.PricePerPiece)
                .NotNull().WithMessage("سعر الحبة مطلوب عند البيع بالحبة")
                .GreaterThan(0).WithMessage("سعر الحبة يجب أن يكون أكبر من صفر")
                .When(x => x.SellBy == SellByType.Piece || x.SellBy == SellByType.Both);

            // لو SellBy تتضمن Package (Package أو Both) → PricePerPackage و PiecesPerPackage إلزاميين
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