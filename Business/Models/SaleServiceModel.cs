using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace Business.Models
{
    public class SaleDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string GroupId { get; set; } = string.Empty;
        public string? GroupName { get; set; }
        public string PaymentMethodId { get; set; } = string.Empty;
        public string? PaymentMethodName { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public decimal Total { get; set; }
        public DateTime SaleDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<SaleItemDto> Items { get; set; } = new List<SaleItemDto>();
    }

    public class SaleItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string SaleId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? ProductBarCode { get; set; }
        public string? ProductSKU { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Observation { get; set; } = string.Empty;
        public string? PromotionId { get; set; }
        public string? PromotionName { get; set; }
        public decimal? PromotionDiscountPercentage { get; set; }
    }


    public class CheckoutRequest
    {
        public string? CustomerId { get; set; }

        [Required]
        public string PaymentMethodId { get; set; } = string.Empty;

        public bool ApplyLoyaltyPoints { get; set; }
        public bool ApplyLoyaltyDiscount { get; set; }

        [Required]
        [MinLength(1)]
        public List<CheckoutItemRequest> Items { get; set; } = new List<CheckoutItemRequest>();
    }

    public class CheckoutItemRequest
    {
        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public string? AppliedPromotionId { get; set; }

        public string? Observation { get; set; }
    }

    public class CheckoutResponse
    {
        public CustomerSummaryDto? Customer { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public List<CheckoutItemResponse> Items { get; set; } = new List<CheckoutItemResponse>();
        public decimal Subtotal { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal Total { get; set; }
        public int? LoyaltyPointsToAdd { get; set; }
        public bool HasSufficientStock { get; set; }
        public List<StockIssueDto>? StockIssues { get; set; }
    }

    public class CustomerSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty;
        public int? LoyaltyPoints { get; set; }
        public string? LoyaltyProgramName { get; set; }
        public decimal? LoyaltyDiscountPercentage { get; set; }
    }

    public class CheckoutItemResponse
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductBarCode { get; set; }
        public string? ProductSKU { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountedUnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Observation { get; set; }
        public PromotionAppliedDto? DefaultPromotionApplied { get; set; }
        public List<PromotionAppliedDto> AvailablePromotions { get; set; } = new List<PromotionAppliedDto>();
        public int StockAvailable { get; set; }
    }

    public class PromotionAppliedDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
    }

    public class StockIssueDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
    }

    public class SaleRequest
    {
        public string? CustomerId { get; set; }

        [Required]
        public string PaymentMethodId { get; set; } = string.Empty;

        public bool ApplyLoyaltyPoints { get; set; }
        public bool ApplyLoyaltyDiscount { get; set; }

        [Required]
        [MinLength(1)]
        public List<SaleItemRequest> Items { get; set; } = new List<SaleItemRequest>();
    }

    public class SaleItemRequest
    {
        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public string? AppliedPromotionId { get; set; }

        public string? Observation { get; set; }
    }

    public class SaleSearchRequest
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string? CustomerId { get; set; }
        public string? UserId { get; set; }
        public string? PaymentMethodId { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;
    }

    public class SaleSearchResponse
    {
        public List<SaleDto> Items { get; set; } = new List<SaleDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pages { get; set; }
        public decimal TotalAmount { get; set; }
    }
}