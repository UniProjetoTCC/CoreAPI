using System;

namespace Business.Models
{
    public class SupplierPriceBusinessModel
    {
        public string Id { get; set; } = string.Empty;
        public required string SupplierId { get; set; }
        public required string ProductId { get; set; }
        public required string GroupId { get; set; }
        public required decimal UnitPrice { get; set; }
        public required int MinimumQuantity { get; set; }
        public required bool IsActive { get; set; } = true;
        public required string SupplierSku { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }

        // Navigation properties
        public ProductBusinessModel? Product { get; set; }
        public SupplierBusinessModel? Supplier { get; set; }
    }
}