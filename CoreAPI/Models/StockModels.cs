using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class StockDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class StockUpdateModel
    {
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0")]
        public int Quantity { get; set; }

        public string? Reason { get; set; }
    }

    public class StockAdjustmentModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        public string? Reason { get; set; }
    }

    public class StockDeductionModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        public string? Reason { get; set; }
    }

    public class StockMovementDto
    {
        public string Id { get; set; } = string.Empty;
        public string StockId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public DateTime MovementDate { get; set; }
        public string? Reason { get; set; }
    }

    public class LowStockSettingsModel
    {
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Threshold must be a positive number")]
        public int Threshold { get; set; } = 10;
    }

    public class StockSearchResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<StockDto> Items { get; set; } = new List<StockDto>();
    }

    // New model to add initial stock when creating a product
    public class InitialStockModel
    {
        [Range(0, int.MaxValue, ErrorMessage = "Initial stock quantity must be greater than or equal to 0")]
        public int Quantity { get; set; } = 0;
    }

    // Cached stock data models
    public class CachedStockData
    {
        public List<StockDto> LowStockItems { get; set; } = new List<StockDto>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class StockMovementSearchResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<StockMovementDto> Items { get; set; } = new List<StockMovementDto>();
    }

    public class CachedStockMovementData
    {
        public List<StockMovementDto> Movements { get; set; } = new List<StockMovementDto>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
