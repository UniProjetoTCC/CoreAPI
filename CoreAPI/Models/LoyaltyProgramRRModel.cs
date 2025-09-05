using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class LoyaltyProgramRequest
    {
        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        [Required]
        [StringLength(500)]
        public required string Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal CentsToPoints { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PointsToCents { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; }
    }

    public class LoyaltyProgramResponse
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public decimal CentsToPoints { get; set; }
        public decimal PointsToCents { get; set; }
        public decimal DiscountPercentage { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
