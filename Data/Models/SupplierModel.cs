using System; 
using System.Collections.Generic; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("Suppliers")]
    public class SupplierModel
    { 
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; } 

        [Required]
        [StringLength(14)]
        public required string Document { get; set; } 

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public required string Email { get; set; } 

        [Required]
        [StringLength(20)]
        [Phone]
        public required string Phone { get; set; } 

        [Required]
        [StringLength(200)]
        public required string Address { get; set; } 

        [Required]
        [StringLength(50)]
        public required string ContactPerson { get; set; } 

        [Required]
        public required bool IsActive { get; set; } = true; 

        [Required]
        [StringLength(20)]
        public required string PaymentTerms { get; set; } 

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
        public DateTime? UpdatedAt { get; set; } 

        public virtual required UserGroupModel UserGroup { get; set; }
        public virtual required ICollection<SupplierPriceModel> SupplierPrices { get; set; } 
        public virtual required ICollection<PurchaseOrderModel> PurchaseOrders { get; set; } 
    } 
}
