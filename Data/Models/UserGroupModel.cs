using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    [Table("UserGroups")]
    public class UserGroupModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GroupId { get; set; }

        [Required]
        [ForeignKey("User")]
        public required string UserId { get; set; }

        [Required]
        [ForeignKey("SubscriptionPlan")]
        public required int SubscriptionPlanId { get; set; }

        [Required]
        public required DateTime SubscriptionStartDate { get; set; }

        [Required]
        public required DateTime SubscriptionEndDate { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual IdentityUser? User { get; set; }
        public virtual SubscriptionPlanModel? SubscriptionPlan { get; set; }
        public virtual ICollection<LinkedUserModel>? LinkedUsers { get; set; }
    }
}