using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("Sales")]
    public class SaleModel
    {
        [Key]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("User")]
        [StringLength(36)]
        public required string UserId { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        [ForeignKey("PaymentMethod")]
        [StringLength(36)]
        public required string PaymentMethodId { get; set; }

        [ForeignKey("Customer")]
        [StringLength(36)]
        public string? CustomerId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal Total { get; set; }

        [Required]
        public required DateTime SaleDate { get; set; } = DateTime.UtcNow;

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual CustomerModel? Customer { get; set; }
        public virtual IdentityUser? User { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
        public virtual PaymentMethodModel? PaymentMethod { get; set; }
        public virtual ICollection<SaleItemModel>? SaleItems { get; set; } = new List<SaleItemModel>();
    }
}
