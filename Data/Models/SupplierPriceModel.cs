using System; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("SupplierPrices")]
    public class SupplierPriceModel
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("Supplier")]
        public required int SupplierId { get; set; } 

        [Required]
        [ForeignKey("Product")]
        public required int ProductId { get; set; } 

        [Required]
        public required string UserGroupId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal UnitPrice { get; set; } 

        [Required]
        [Range(1, int.MaxValue)]
        public required int MinimumQuantity { get; set; } 

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        [StringLength(50)]
        public required string SupplierSku { get; set; }

        public virtual required SupplierModel Supplier { get; set; }
        public virtual required ProductModel Product { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

        public DateTime? ValidUntil { get; set; }
    } 
}
