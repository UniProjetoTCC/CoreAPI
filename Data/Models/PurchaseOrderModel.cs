using System; 
using System.Collections.Generic; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("PurchaseOrders")]
    public class PurchaseOrderModel
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("Supplier")]
        public required int SupplierId { get; set; }

        [Required]
        public required string UserGroupId { get; set; }

        [Required]
        [StringLength(50)]
        public required string OrderNumber { get; set; }

        [Required]
        public required DateTime OrderDate { get; set; }

        [Required]
        [StringLength(20)]
        public required string Status { get; set; } = "Pending";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal TotalAmount { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public virtual required SupplierModel Supplier { get; set; } 

        [Required]
        public virtual required ICollection<PurchaseOrderItemModel> Items { get; set; } = new List<PurchaseOrderItemModel>(); 
    } 
}