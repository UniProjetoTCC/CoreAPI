using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("LinkedUsers")]
    public class LinkedUserModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("ParentUser")]
        public required string ParentUserId { get; set; }

        [Required]
        [ForeignKey("LinkedUser")]
        public required string LinkedUserId { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; }

        [Required]
        public required bool CanPerformTransactions { get; set; }

        [Required]
        public required bool CanGenerateReports { get; set; }

        [Required]
        public required bool CanManageProducts { get; set; }

        [Required]
        public required bool CanAlterStock { get; set; }

        [Required]
        public required bool CanManagePromotions { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual IdentityUser? ParentUser { get; set; }
        public virtual IdentityUser? LinkedUser { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
    }
}
