using System.ComponentModel.DataAnnotations;
using Business.Models;

namespace CoreAPI.Models
{
    public class PromotionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductInPromotionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string BarCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal PromotionalPrice { get; set; }
    }

    public class PromotionSearchResponse
    {
        public List<PromotionDto> Items { get; set; } = new List<PromotionDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pages { get; set; }
        public bool FromCache { get; set; }
    }

    public class CreatePromotionRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdatePromotionRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }

    public class AddProductToPromotionRequest
    {
        [Required]
        [StringLength(36)]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        public string PromotionId { get; set; } = string.Empty;
    }

    public class AddProductsToPromotionRequest
    {
        [Required]
        public List<string> ProductIds { get; set; } = new List<string>();

        [Required]
        [StringLength(36)]
        public string PromotionId { get; set; } = string.Empty;
    }

    public class RemoveProductFromPromotionRequest
    {
        [Required]
        [StringLength(36)]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        public string PromotionId { get; set; } = string.Empty;
    }

    public class RemoveProductsFromPromotionRequest
    {
        [Required]
        public List<string> ProductIds { get; set; } = new List<string>();

        [Required]
        [StringLength(36)]
        public string PromotionId { get; set; } = string.Empty;
    }

    public class CachedPromotionSearch
    {
        public List<PromotionDto> Promotions { get; set; } = new List<PromotionDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
