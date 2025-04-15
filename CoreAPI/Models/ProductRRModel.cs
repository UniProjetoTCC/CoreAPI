using System.ComponentModel.DataAnnotations;
using System;
using Business.Models;
using System.Collections.Generic;

namespace CoreAPI.Models
{
    public class ProductCreateModel
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string CategoryName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string BarCode { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [Range(0.01, 1000000)]
        public decimal Price { get; set; }
        
        [Required]
        [Range(0.01, 1000000)]
        public decimal Cost { get; set; }
        
        public bool Active { get; set; } = true;
    }

    public class ProductUpdateModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        [StringLength(36)]
        public string CategoryId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string BarCode { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [Range(0.01, 1000000)]
        public decimal Price { get; set; }
        
        [Required]
        [Range(0.01, 1000000)]
        public decimal Cost { get; set; }
        
        public bool Active { get; set; } = true;
    }

    public class ProductSearchResponse
    {
        public List<ProductBusinessModel> Items { get; set; } = new List<ProductBusinessModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pages { get; set; }
        public bool FromCache { get; set; }
    }

    public class CachedProductSearch
    {
        public List<ProductBusinessModel> Products { get; set; } = new List<ProductBusinessModel>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}