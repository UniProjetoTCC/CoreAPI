namespace Business.Models
{
    public class SupplierPriceBusinessModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SupplierId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int MinimumQuantity { get; set; }
        public bool IsActive { get; set; }
        public string SupplierSku { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }

        public ProductBusinessModel? Product { get; set; }
    }
}