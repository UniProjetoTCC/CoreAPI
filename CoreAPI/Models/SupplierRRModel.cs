using Business.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class SupplierDto
    {
        public string Id { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
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

    public class CreateSupplierRequest
    {
        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        [Required]
        [StringLength(14)]
        public required string Document { get; set; }

        [Required]
        [StringLength(50)]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [StringLength(15)]
        [Phone]
        public required string Phone { get; set; }

        [Required]
        [StringLength(100)]
        public required string Address { get; set; }

        [Required]
        [StringLength(50)]
        public required string ContactPerson { get; set; }

        [Required]
        [StringLength(20)]
        public required string PaymentTerms { get; set; }
    }

    public class UpdateSupplierRequest
    {
        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        [Required]
        [StringLength(14)]
        public required string Document { get; set; }

        [Required]
        [StringLength(50)]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [StringLength(15)]
        [Phone]
        public required string Phone { get; set; }

        [Required]
        [StringLength(100)]
        public required string Address { get; set; }

        [Required]
        [StringLength(50)]
        public required string ContactPerson { get; set; }

        [Required]
        [StringLength(20)]
        public required string PaymentTerms { get; set; }
    }

    public class SupplierSearchResponse
    {
        public List<SupplierDto> Items { get; set; } = new List<SupplierDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pages { get; set; }
        public bool FromCache { get; set; }
    }

    public class CachedSupplierSearch
    {
        public List<SupplierBusinessModel> Items { get; set; } = new List<SupplierBusinessModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public DateTime Timestamp { get; set; }
    }
}