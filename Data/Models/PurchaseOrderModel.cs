using System; 
using System.Collections.Generic; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("PurchaseOrders")]
    public class PurchaseOrderModel
    { 
        // Identificadores
        [Key] 
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("Supplier")]
        [StringLength(36)]
        public required string SupplierId { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        [StringLength(50)]
        public required string OrderNumber { get; set; }

        [Required]
        public required DateTime OrderDate { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal TotalAmount { get; set; }

        public DateTime? DeliveryDate { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual SupplierModel? Supplier { get; set; } 
        public virtual UserGroupModel? UserGroup { get; set; }
        public virtual ICollection<PurchaseOrderItemModel>? Items { get; set; } = new List<PurchaseOrderItemModel>(); 
    } 
}
