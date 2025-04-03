using System; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 
using System.Collections.Generic;

namespace Data.Models
{
    [Table("Customers")]
    public class CustomerModel
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [ForeignKey("UserGroup")]
        [Required]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        [StringLength(14)]
        public required string Document { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [ForeignKey("LoyaltyProgram")]
        [StringLength(36)]
        public string? LoyaltyProgramId { get; set; }

        [Required]
        public required bool IsActive { get; set; } = true;

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual UserGroupModel? UserGroup { get; set; }
        public virtual LoyaltyProgramModel? LoyaltyProgram { get; set; }
        public virtual ICollection<SaleModel>? Sales { get; set; } = new List<SaleModel>();
    }
}
