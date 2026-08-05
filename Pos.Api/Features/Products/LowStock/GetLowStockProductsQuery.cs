using MediatR;

namespace Pos.Api.Features.Products.LowStock
{
    public class GetLowStockProductsQuery : IRequest<List<LowStockProductDto>>
    {
        // الحد الأدنى الافتراضي — قابل للتعديل لاحقاً (مثلاً يصير حقل بجدول Products
        // بدل ما يكون ثابت عام، لو الفريق قرر هيك)
        public int Threshold { get; set; } = 10;

        // true = بس الأصناف يلي خلصت تماماً (رصيدها = صفر)
        public bool OnlyOutOfStock { get; set; } = false;
    }
}