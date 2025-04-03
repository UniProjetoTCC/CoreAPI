using System; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("PaymentMethods")]
    public class PaymentMethodModel 
    { 
        [Key]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [ForeignKey("UserGroup")]
        [Required]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        [Required]
        [StringLength(50)]
        public required string Code { get; set; }

        [Required]
        [StringLength(500)]
        public required string Description { get; set; }

        [Required]
        public required bool Active { get; set; } = true;

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<SaleModel>? Sales { get; set; } = new List<SaleModel>();
        public virtual UserGroupModel? UserGroup { get; set; }
    } 
}
