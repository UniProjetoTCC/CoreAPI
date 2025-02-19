using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("ProductExpirations")]
    public class ProductExpirationModel
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("Product")]
        public required int ProductId { get; set; } 

        [Required]
        [ForeignKey("Stock")]
        public required int StockId { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; }

        [Required]
        public required DateTime ExpirationDate { get; set; } 

        [Required]
        [Range(0, int.MaxValue)]
        public required int Quantity { get; set; }

        [Required]
        [StringLength(100)]
        public required string BatchNumber { get; set; }

        [Required]
        public required bool IsActive { get; set; } = false;

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual required ProductModel Product { get; set; }
        public virtual required StockModel Stock { get; set; }
        public virtual required UserGroupModel UserGroup { get; set; }
    }
}
