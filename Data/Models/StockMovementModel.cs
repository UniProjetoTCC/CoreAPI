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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required] 
        [ForeignKey("Stock")]
        public required int StockId { get; set; } 

        [Required] 
        [ForeignKey("User")]
        public required string UserId { get; set; }

        [Required] 
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; } 

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
