using System;
using System.Collections.Generic;

namespace Business.Models
{
    public class ProductBusinessModel
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string BarCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Instead of using complex model references, use simple properties
        public string? CategoryName { get; set; }
        public string? GroupName { get; set; }
    }
}