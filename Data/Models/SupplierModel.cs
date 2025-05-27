using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("Suppliers")]
    public class SupplierModel
    {
        [Key]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        [Required]
        [StringLength(14)]
        public required string Document { get; set; }

        [Required]
        [StringLength(50)]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [StringLength(15)]
        [Phone]
        public required string Phone { get; set; }

        [Required]
        [StringLength(100)]
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

        public virtual UserGroupModel? UserGroup { get; set; }
        public virtual ICollection<SupplierPriceModel>? SupplierPrices { get; set; }
        public virtual ICollection<PurchaseOrderModel>? PurchaseOrders { get; set; }
    }
}
