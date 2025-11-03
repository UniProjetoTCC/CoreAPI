using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class SupplierPriceDto
    {
        public string Id { get; set; } = string.Empty;
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty; // Denormalized
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty; // Denormalized
        public decimal UnitPrice { get; set; }
        public int MinimumQuantity { get; set; }
        public bool IsActive { get; set; }
        public string SupplierSku { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // CreateSupplierPriceRequest foi REMOVIDO daqui.

    public class UpdateSupplierPriceRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0.")]
        public required decimal UnitPrice { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Minimum quantity must be at least 1.")]
        public required int MinimumQuantity { get; set; }

        [Required]
        [StringLength(50)]
        public required string SupplierSku { get; set; }

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
    }

    public class SupplierPriceSearchResponse
    {
        public List<SupplierPriceDto> Items { get; set; } = new List<SupplierPriceDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pages { get; set; }
    }
}