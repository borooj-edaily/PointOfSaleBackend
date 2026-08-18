namespace Pos.Api.Features.Customers.GetAll
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }

        // عدد كل الفواتير المسجّلة على الزبون (دين أو كاش، مسدد أو لأ).
        // العمود موجود بالاستعلام (InvoiceCount) بس كان ناقص من هون، فكان
        // Dapper بيتجاهله وبيرجع 0 دايماً لكل الزبائن بالواجهة.
        public int InvoiceCount { get; set; }

        // مجموع كل الفواتير (Total) المسجّلة على الزبون، بغض النظر إذا
        // كانت دين أو كاش — هاي "المشتريات" الظاهرة بصفحة الزبائن.
        // نفس المشكلة: كان ناقص من الـ DTO فكان يرجع 0.
        public decimal TotalPurchases { get; set; }

        // مجموع الفواتير المسجّلة عليه كدين وما زالت غير مسددة — هاد يلي
        // بيخلي صفحة الزبائن تبين "ملف الزبون" وقديش عليه بلمحة وحدة
        // بدون ما تدخل لكل فاتورة لحالها.
        public decimal OutstandingDebt { get; set; }
    }
}