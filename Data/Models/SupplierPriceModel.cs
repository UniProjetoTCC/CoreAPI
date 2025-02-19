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
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal UnitPrice { get; set; } 

        [Required]
        [Range(1, int.MaxValue)]
        public required int MinimumQuantity { get; set; } 

        [Required]
        public required bool IsActive { get; set; } = true;

        [Required]
        [StringLength(50)]
        public required string SupplierSku { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [Required]
        public required DateTime ValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime? ValidUntil { get; set; }

        public virtual required SupplierModel Supplier { get; set; }
        public virtual required ProductModel Product { get; set; }
        public virtual required UserGroupModel UserGroup { get; set; }
    } 
}
