namespace Pos.Api.Models
{
    // للجداول اللي بتتعدل (Category, Product)
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedByUserId { get; set; }
    }

    // للجداول الثابتة/التاريخية (StockMovement)
    public abstract class ImmutableEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
    }
}