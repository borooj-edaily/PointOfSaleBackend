using System.Runtime.Intrinsics.X86;

namespace POINTOFSALEBACKEND.Models
{
    public class Category : AuditableEntity
    {

        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public ICollection<Product> Products { get; set; }
            = new List<Product>();






    }
}
