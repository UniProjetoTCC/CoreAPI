using Business.Models;
using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class PaymentMethodRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public required string Name { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public required string Code { get; set; }

        [Required]
        [StringLength(500)]
        public required string Description { get; set; }
    }

    public class PaymentMethodResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}