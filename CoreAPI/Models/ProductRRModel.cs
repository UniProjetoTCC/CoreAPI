using System.ComponentModel.DataAnnotations;
using System;

namespace CoreAPI.Models
{
    public class GetByID
    {
        [Required]
        public int Id { get; set; }
    }

    public class ProductCreateModel
    {
        [Required]
        public int CategoryId { get; set; }
        
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
        public int Id { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
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
}