using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("SubscriptionPlans")]
    public class SubscriptionPlanModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public required string UserGroupId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }  // Standard, Premium, Enterprise

        [Required]
        [Range(0, 1000)]
        public int LinkedUserLimit { get; set; }

        [Required]
        public bool Requires2FA { get; set; }

        [Required]
        public bool HasPremiumSupport { get; set; }

        [Required]
        public bool HasAdvancedAnalytics { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal MonthlyPrice { get; set; }

        [Required]
        [StringLength(500)]
        public required string Description { get; set; }

        [Required]
        public required bool IsActive { get; set; } = true;

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual required ICollection<UserSubscriptionModel> UserSubscriptions { get; set; }
    }
}
