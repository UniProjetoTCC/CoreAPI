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
        [StringLength(50)]
        public required string Name { get; set; }  // Standard, Premium, Enterprise

        [Required]
        [Range(0, 1000)]
        public required int LinkedUserLimit { get; set; }

        [Required]
        public required bool Requires2FA { get; set; }

        [Required]
        public required bool HasPremiumSupport { get; set; }

        [Required]
        public required bool HasAdvancedAnalytics { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal MonthlyPrice { get; set; }

        [Required]
        public required bool IsActive { get; set; } = false;

        [Required]
        public required DateTime EndDate { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<UserGroupModel> UserGroups { get; set; } = new List<UserGroupModel>();
    }
}