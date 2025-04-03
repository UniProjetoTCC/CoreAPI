using System; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("SupplierPrices")]
    public class SupplierPriceModel
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("Supplier")]
        [StringLength(36)]
        public required string SupplierId { get; set; } 

        [Required]
        [ForeignKey("Product")]
        [StringLength(36)]
        public required string ProductId { get; set; } 

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

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

        public virtual SupplierModel? Supplier { get; set; }
        public virtual ProductModel? Product { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
    } 
}
