namespace Pos.Api.Features.Categories.GetAll
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public int ProductsCount { get; set; }
    }
}