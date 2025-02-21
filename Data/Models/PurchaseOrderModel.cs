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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("Supplier")]
        public required int SupplierId { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; }

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
