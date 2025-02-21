using System; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("PurchaseOrderItems")]
    public class PurchaseOrderItemModel
    { 
        // Identificadores
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("PurchaseOrder")]
        public required int PurchaseOrderId { get; set; } 

        [Required]
        [ForeignKey("Product")]
        public required int ProductId { get; set; } 

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; } 

        [Required]
        [Range(1, int.MaxValue)]
        public required int Quantity { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal UnitPrice { get; set; } 

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
        public DateTime? UpdatedAt { get; set; } 

        public virtual PurchaseOrderModel? PurchaseOrder { get; set; } 
        public virtual ProductModel? Product { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
    } 
}
