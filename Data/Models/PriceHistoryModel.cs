using System; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 
using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    [Table("PriceHistories")]
    public class PriceHistoryModel 
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("Product")]
        public required int ProductId { get; set; } 

        [Required]
        public required string UserGroupId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal OldPrice { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal NewPrice { get; set; } 

        [Required]
        public required DateTime ChangeDate { get; set; } = DateTime.UtcNow; 

        [StringLength(200)]
        public string? Reason { get; set; }

        [Required]
        [ForeignKey("ChangedByUser")]
        public required string ChangedByUserId { get; set; }

        public virtual required ProductModel Product { get; set; }

        public virtual required IdentityUser ChangedByUser { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    } 
}