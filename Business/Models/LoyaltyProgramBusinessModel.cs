using System;
using System.Collections.Generic;

namespace Business.Models
{
    public class LoyaltyProgramBusinessModel
    {
        public required string Id { get; set; }
        public required string GroupId { get; set; }
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
