using System; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("PurchaseOrderItems")]
    public class PurchaseOrderItemModel
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("Order")]
        public required int OrderId { get; set; } 

        [Required]
        [ForeignKey("Product")]
        public required int ProductId { get; set; } 

        [Required]
        public required string UserGroupId { get; set; } 

        [Required]
        [Range(1, int.MaxValue)]
        public required int Quantity { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal UnitPrice { get; set; } 

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow; 

        public DateTime? UpdatedAt { get; set; } 

        [Required]
        public virtual required PurchaseOrderModel Order { get; set; } 

        [Required]
        public virtual required ProductModel Product { get; set; } 
    } 
}
