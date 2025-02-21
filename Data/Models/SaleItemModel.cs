using System; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("SaleItems")]
    public class SaleItemModel
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("Sale")]
        public required int SaleId { get; set; } 

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
        [Range(0, double.MaxValue)]
        public required decimal UnitPrice { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal DiscountAmount { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public required decimal TotalAmount { get; set; } 

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
        public DateTime? UpdatedAt { get; set; } 

        public virtual SaleModel? Sale { get; set; } 
        public virtual ProductModel? Product { get; set; } 
        public virtual UserGroupModel? UserGroup { get; set; }
    }
}
