namespace POINTOFSALEBACKEND.Models
{
    public abstract class AuditableEntity : BaseEntity
    {
        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedByUserId { get; set; }
    }

}
