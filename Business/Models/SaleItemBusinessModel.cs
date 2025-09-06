using System;

namespace Business.Models
{
    public class SaleItemBusinessModel
    {
        public string Id { get; set; } = string.Empty;
        public string SaleId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Observation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ProductBusinessModel? Product { get; set; }
        public SaleBusinessModel? Sale { get; set; }
        public string? PromotionId { get; set; }
        public string? PromotionName { get; set; }
        public decimal? PromotionDiscountPercentage { get; set; }
    }
}
