using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class CustomerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? LoyaltyProgramId { get; set; }
        public int LoyaltyPoints { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateCustomerRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public required string Name { get; set; }

        [Required]
        [StringLength(14, MinimumLength = 11)]
        [RegularExpression(@"^\d{11,14}$", ErrorMessage = "Document must contain only numbers and be between 11 and 14 digits")]
        public required string Document { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Phone must contain only numbers and be between 10 and 15 digits")]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }
    }

    public class UpdateCustomerRequest
    {
        [Required]
        public required string Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public required string Name { get; set; }

        [Required]
        [StringLength(14, MinimumLength = 11)]
        [RegularExpression(@"^\d{11,14}$", ErrorMessage = "Document must contain only numbers and be between 11 and 14 digits")]
        public required string Document { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Phone must contain only numbers and be between 10 and 15 digits")]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }
    }

    public class CustomerSearchResponse
    {
        public List<CustomerDto> Items { get; set; } = new List<CustomerDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pages { get; set; }
        public bool FromCache { get; set; }
    }

    public class LinkLoyaltyProgramRequest
    {
        [Required]
        public required string CustomerId { get; set; }

        [Required]
        public required string LoyaltyProgramId { get; set; }
    }

    public class AddPointsRequest
    {
        [Required]
        public required string CustomerId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public required decimal Amount { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }
    }

    public class RemovePointsRequest
    {
        [Required]
        public required string CustomerId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public required decimal Amount { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }
    }

    public class RemovePointsDirectRequest
    {
        [Required]
        public required string CustomerId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than zero")]
        public required int Points { get; set; }
    }

    public class CustomerPointsResponse
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int Points { get; set; }
        public int PointsAdded { get; set; }
        public int CurrentPoints { get; set; }
        public string? LoyaltyProgramId { get; set; }
        public string? LoyaltyProgramName { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal TransactionAmount { get; set; }
        public string? TransactionDescription { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    }

    public class CachedCustomerSearch
    {
        public List<CustomerDto> Items { get; set; } = new List<CustomerDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AdminRemovePointsRequest
    {
        [Required]
        public required string CustomerId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than zero")]
        public required int Points { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Description { get; set; }
    }
}
