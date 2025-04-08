using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Data.Models
{
    [Table("Promotions")]
    public class PromotionModel 
    { 
        [Key] 
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100)]
        public required decimal DiscountPercentage { get; set; }

        [Required]
        public required DateTime StartDate { get; set; }

        [Required]
        public required DateTime EndDate { get; set; }

        [Required]
        public required bool IsActive { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual UserGroupModel? UserGroup { get; set; }
        public virtual ICollection<ProductPromotionModel>? ProductPromotions { get; set; } = new List<ProductPromotionModel>();
    } 
}
