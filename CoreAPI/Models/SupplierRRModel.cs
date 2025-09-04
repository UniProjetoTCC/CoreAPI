using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class SupplierDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SupplierCreateModel
    {
        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(14)]
        public string Document { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(50)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Address { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string ContactPerson { get; set; } = string.Empty;

        [StringLength(20)]
        public string PaymentTerms { get; set; } = "On delivery";

        public bool IsActive { get; set; } = true;
    }

    public class SupplierUpdateModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(14)]
        public string Document { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(50)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Address { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string ContactPerson { get; set; } = string.Empty;

        [StringLength(20)]
        public string PaymentTerms { get; set; } = "On delivery";

        public bool IsActive { get; set; } = true;
    }

    public class SupplierSearchResponse
    {
        public List<SupplierDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pages { get; set; }
    }

    // --- DTOs para SupplierPrice ---

    public class SupplierPriceDto
    {
        public string Id { get; set; } = string.Empty;
        public string SupplierId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int MinimumQuantity { get; set; }
        public string SupplierSku { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
    }

    public class SupplierPriceCreateModel
    {
        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(1, int.MaxValue)]
        public int MinimumQuantity { get; set; } = 1;

        [Required, StringLength(50)]
        public string SupplierSku { get; set; } = string.Empty;

        public DateTime? ValidUntil { get; set; }
    }

    public class SupplierPriceUpdateModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(1, int.MaxValue)]
        public int MinimumQuantity { get; set; } = 1;

        [Required, StringLength(50)]
        public string SupplierSku { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime? ValidUntil { get; set; }
    }
}