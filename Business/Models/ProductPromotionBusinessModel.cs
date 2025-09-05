using System;

namespace Business.Models
{
    public class ProductPromotionBusinessModel
    {
        public required string Id { get; set; }
        public required string ProductId { get; set; }
        public required string PromotionId { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required ProductBusinessModel Product { get; set; }
        public required PromotionBusinessModel Promotion { get; set; }
    }
}
