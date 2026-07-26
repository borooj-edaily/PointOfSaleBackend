namespace Pos.Api.Models
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}