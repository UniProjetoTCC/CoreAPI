using System; 
using System.Collections.Generic; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("Stock")]
    public class StockModel
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("Product")]
        public required int ProductId { get; set; } 

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public required decimal Quantity { get; set; } 

        [StringLength(50)]
        public string? Location { get; set; } 

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual required ProductModel Product { get; set; }
        public virtual required UserGroupModel UserGroup { get; set; }
        public virtual required ICollection<StockMovementModel> StockMovements { get; set; } = new List<StockMovementModel>();
        public virtual required ICollection<ProductExpirationModel> ProductExpirations { get; set; } = new List<ProductExpirationModel>();
    } 
}
