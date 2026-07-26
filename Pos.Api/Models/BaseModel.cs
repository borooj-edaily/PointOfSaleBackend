namespace POINTOFSALEBACKEND.Models
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? CreatedByUserId { get; set; }
    }
}
