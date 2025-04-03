using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    [Table("PriceHistories")]
    public class PriceHistoryModel
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
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        [ForeignKey("ChangedByUser")]
        [StringLength(36)]
        public required string ChangedByUserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal OldPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal NewPrice { get; set; }

        [Required]
        public required DateTime ChangeDate { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? Reason { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ProductModel? Product { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
        public virtual IdentityUser? ChangedByUser { get; set; }
    }
}
