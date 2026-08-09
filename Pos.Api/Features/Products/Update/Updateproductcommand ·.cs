using MediatR;
using Pos.Api.Enums;

namespace Pos.Api.Features.Products.Update
{
    // ملاحظة: التعديل هون بيغطي بيانات الصنف (الاسم/الكاتيجوري/طريقة البيع/الأسعار)
    // فقط. الـ StockInPieces ما بينعدّل من هون أبداً — أي تغيير على المخزون الفعلي
    // لازم يمر عبر StockMovements (Restock/Deduct) حتى يضل في سجل تاريخي لكل حركة.
    public class UpdateProductCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int CategoryId { get; set; }
        public SellByType SellBy { get; set; }
        public int? PiecesPerPackage { get; set; }
        public decimal? PricePerPiece { get; set; }
        public decimal? PricePerPackage { get; set; }

        // بيتحدد من الـ Controller (من التوكن/الـ User الحالي)، مش من الـ Frontend
        public int? UpdatedByUserId { get; set; }
    }
}