using System; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 
using Microsoft.AspNetCore.Identity; 

namespace Data.Models
{
    [Table("StockMovements")]
    public class StockMovementModel 
    { 
        [Key]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required] 
        [ForeignKey("Stock")]
        [StringLength(36)]
        public required string StockId { get; set; } 

        [Required] 
        [ForeignKey("User")]
        [StringLength(36)]
        public required string UserId { get; set; }

        [Required] 
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; } 

        [Required] 
        [StringLength(20)]
        public required string MovementType { get; set; } 

        [Required] 
        [Range(1, int.MaxValue)]
        public required int Quantity { get; set; } 

        [StringLength(200)]
        public string? Reason { get; set; }

        [Required]
        public required DateTime MovementDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? ReferenceNumber { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual StockModel? Stock { get; set; }
        public virtual IdentityUser? User { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
    } 
}
