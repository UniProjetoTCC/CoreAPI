using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    [Table("Sales")]
    public class SaleModel
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required] 
        [ForeignKey("User")]
        public required string UserId { get; set; } 

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; } 

        [Required]
        [ForeignKey("PaymentMethod")]
        public required int PaymentMethodId { get; set; } 

        [ForeignKey("Customer")]
        public int? CustomerId { get; set; } 

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
