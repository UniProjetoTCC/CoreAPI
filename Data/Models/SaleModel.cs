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
        public required string UserGroupId { get; set; }

        public virtual required IdentityUser User { get; set; } 

        [ForeignKey("Customer")]
        public int? CustomerId { get; set; } 

        public virtual CustomerModel? Customer { get; set; } 

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal Total { get; set; } 

        [Required]
        public required DateTime SaleDate { get; set; } = DateTime.UtcNow; 

        [Required]
        [ForeignKey("PaymentMethod")]
        public required int PaymentMethodId { get; set; } 

        public virtual required PaymentMethodModel PaymentMethod { get; set; } 

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual required ICollection<SaleItemModel> SaleItems { get; set; } = new List<SaleItemModel>(); 
    } 
}
