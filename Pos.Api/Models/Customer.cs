namespace Pos.Api.Models
{
    public class Customer : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}