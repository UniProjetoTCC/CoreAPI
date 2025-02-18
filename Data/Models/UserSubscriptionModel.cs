using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    [Table("UserSubscriptions")]
    public class UserSubscriptionModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public required string UserId { get; set; }

        [Required]
        public required string UserGroupId { get; set; }

        [Required]
        [ForeignKey("SubscriptionPlan")]
        public required int SubscriptionPlanId { get; set; }

        [Required]
        public required DateTime StartDate { get; set; }

        [Required]
        public required DateTime EndDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public required string PaymentStatus { get; set; } = "Pending";

        [Required]
        public required bool Active { get; set; } = true;

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual required IdentityUser User { get; set; }
        public virtual required SubscriptionPlanModel SubscriptionPlan { get; set; }
    }
}
