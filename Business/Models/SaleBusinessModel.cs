using System;
using System.Collections.Generic;

namespace Business.Models
{
    public class SaleBusinessModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string PaymentMethodId { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public decimal Total { get; set; }
        public DateTime SaleDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public CustomerBusinessModel? Customer { get; set; }
        public string? UserName { get; set; }
        public string? GroupName { get; set; }
        public string? PaymentMethodName { get; set; }
        public List<SaleItemBusinessModel> SaleItems { get; set; } = new List<SaleItemBusinessModel>();
    }
}
