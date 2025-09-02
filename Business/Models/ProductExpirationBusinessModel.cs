using System;

namespace Business.Models
{
    public class ProductExpirationBusinessModel
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string StockId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public string? Location { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ProductBusinessModel? Product { get; set; }
        public StockBusinessModel? Stock { get; set; }
    }
}
