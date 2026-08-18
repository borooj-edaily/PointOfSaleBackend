namespace Pos.Api.Features.Customers.GetAll
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }

        // مجموع الفواتير المسجّلة عليه كدين وما زالت غير مسددة — هاد يلي
        // بيخلي صفحة الزبائن تبين "ملف الزبون" وقديش عليه بلمحة وحدة
        // بدون ما تدخل لكل فاتورة لحالها.
        public decimal OutstandingDebt { get; set; }
    }
}
