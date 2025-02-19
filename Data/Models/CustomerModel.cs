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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [ForeignKey("UserGroup")]
        [Required]
        public required int GroupId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        [StringLength(14)]
        public required string Document { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [StringLength(20)]
        [Phone]
        public required string Phone { get; set; }

        [StringLength(200)]
        public required string Address { get; set; }

        [ForeignKey("LoyaltyProgram")]
        public int? LoyaltyProgramId { get; set; }

        [Required]
        public required bool Active { get; set; } = true;

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual LoyaltyProgramModel? LoyaltyProgram { get; set; }
        public virtual required UserGroupModel UserGroup { get; set; }
        public virtual ICollection<SaleModel> Sales { get; set; } = new List<SaleModel>();
    }
}
