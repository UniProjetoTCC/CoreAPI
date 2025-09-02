using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Business.Models;

namespace CoreAPI.Models
{
    public class ProductExpirationDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string StockId { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public string? Location { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ProductDto? Product { get; set; }
        public StockDto? Stock { get; set; }
    }

    public class CreateProductExpirationModel
    {
        [Required]
        public string ProductId { get; set; } = string.Empty;
        
        [Required]
        public string StockId { get; set; } = string.Empty;
        
        [Required]
        public DateTime ExpirationDate { get; set; }
        
        public string? Location { get; set; }
        
        public bool IsActive { get; set; } = true;
    }

    public class UpdateProductExpirationModel
    {
        [Required]
        public DateTime ExpirationDate { get; set; }
        
        public string? Location { get; set; }
        
        public bool IsActive { get; set; }
    }

    public class ProductExpirationSearchResponse
    {
        public List<ProductExpirationDto> Items { get; set; } = new List<ProductExpirationDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pages { get; set; }
        public bool FromCache { get; set; }
    }

    public class CachedProductExpirationSearch
    {
        public List<ProductExpirationBusinessModel> ProductExpirations { get; set; } = new List<ProductExpirationBusinessModel>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
