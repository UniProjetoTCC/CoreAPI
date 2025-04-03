using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    public class UserPaymentCardModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("User")]
        [StringLength(36)]
        public required string UserId { get; set; }
        
        [Required]
        [CreditCard]
        [ProtectedPersonalData]
        public required string CardNumber { get; set; }

        [Required]
        [StringLength(100)]
        public required string CardholderName { get; set; }

        [Required]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2}|[0-9]{4})$", ErrorMessage = "Invalid expiration date format: MM/YY ou MM/YYYY")]
        public required string ExpirationDate { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "CVV must be 3 digits.")]
        [ProtectedPersonalData]
        public required string CVV { get; set; }

        [Required]
        public required CardType Type { get; set; }

        public virtual IdentityUser? User { get; set; }
    }

    public enum CardType
    {
        Debit,
        Credit,
        Both
    }
}
