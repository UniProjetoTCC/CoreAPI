using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("ProductExpirations")]
    public class ProductExpirationModel
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("Product")]
        [StringLength(36)]
        public required string ProductId { get; set; } 

        [Required]
        [ForeignKey("Stock")]
        [StringLength(36)]
        public required string StockId { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        public required DateTime ExpirationDate { get; set; } 

        [Required]
        [Range(0, int.MaxValue)]
        public required int Quantity { get; set; }

        [Required]
        [StringLength(100)]
        public required string BatchNumber { get; set; }

        [StringLength(50)]
        public required string? Location{get; set;}

        [Required]
        public required bool IsActive { get; set; } = false;

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ProductModel? Product { get; set; }
        public virtual StockModel? Stock { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
    }
}
