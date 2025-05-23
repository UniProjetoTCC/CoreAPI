using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("SubscriptionPlans")]
    public class SubscriptionPlanModel
    {
        [Key]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public required string Name { get; set; }  // Standard, Premium, Enterprise

        [Required]
        [Range(0, 1000)]
        public required int LinkedUserLimit { get; set; }

        [Required]
        public required bool IsActive { get; set; } = false;

        [Required]
        public required bool Requires2FA { get; set; }

        [Required]
        public required bool RequiresEmailVerification { get; set; }

        [Required]
        public required bool HasPremiumSupport { get; set; }

        [Required]
        public required bool HasAdvancedAnalytics { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal MonthlyPrice { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<UserGroupModel>? UserGroups { get; set; } = new List<UserGroupModel>();
    }
}