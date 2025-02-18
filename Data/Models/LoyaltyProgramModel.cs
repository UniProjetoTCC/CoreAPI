using System; 
using System.Collections.Generic; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("LoyaltyPrograms")]
    public class LoyaltyProgramModel 
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        public required string UserGroupId { get; set; }

        [Required, StringLength(100)] 
        public required string Name { get; set; } = string.Empty; 

        [Required]
        [StringLength(500)]
        public required string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal PointsPerCurrency { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal RedemptionRate { get; set; }

        [Range(0, int.MaxValue)] 
        public int Points { get; set; } 

        [Range(0, 100)] 
        public decimal DiscountPercentage { get; set; }

        [Required]
        public required bool Active { get; set; } = true;

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual required ICollection<CustomerModel> Customers { get; set; } = new List<CustomerModel>();
    }
}
