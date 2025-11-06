using System;
using System.Collections.Generic;

namespace Business.Models
{
    public class PurchaseOrderBusinessModel
    {
        public string Id { get; set; } = string.Empty;
        public string SupplierId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string? Status { get; set; } // Ex: Pending, Completed, Cancelled
        public decimal TotalAmount { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public SupplierBusinessModel? Supplier { get; set; }
        public List<PurchaseOrderItemBusinessModel> Items { get; set; } = new();
    }
}