using System;

namespace Business.Models
{
    public class PurchaseOrderItemBusinessModel
    {
        public string Id { get; set; } = string.Empty;
        public string PurchaseOrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Navigation Properties
        public ProductBusinessModel? Product { get; set; }
        public PurchaseOrderBusinessModel? PurchaseOrder { get; set; }
    }
}