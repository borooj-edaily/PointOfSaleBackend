using MediatR;

namespace Pos.Api.Features.Customers.GetAll
{
    public class GetAllCustomersQuery : IRequest<List<CustomerDto>>
    {
        // فلترة اختيارية: هل بدك بس الفعّالين ولا الكل
        public bool OnlyActive { get; set; } = false;

        // بحث حر بالاسم أو رقم الهاتف — مستخدم من شاشة الكاشير (typeahead
        // عند تسجيل دين) ومن صفحة إدارة الزبائن.
        public string? Search { get; set; }
    }
}
