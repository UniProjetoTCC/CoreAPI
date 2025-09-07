namespace CoreAPI.Models
{
    // Sales Report Models
    public class SalesReportResponse
    {
        public GeneralReportInfo GeneralInfo { get; set; } = new();
        public SalesSummary SalesSummary { get; set; } = new();
        public List<ProductSalesInfo> TopSellingProducts { get; set; } = new();
        public List<CategorySalesInfo> TopSellingCategories { get; set; } = new();
    }

    // Inventory Report Models
    public class InventoryReportResponse
    {
        public GeneralReportInfo GeneralInfo { get; set; } = new();
        public InventorySummary InventorySummary { get; set; } = new();
        public List<ProductStockInfo> ProductsWithLowStock { get; set; } = new();
        public List<ProductStockInfo> ProductsWithHighStock { get; set; } = new();
    }

    // General Components for Reports
    public class GeneralReportInfo
    {
        public string ReportName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateRange? ReportPeriod { get; set; }
    }

    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class SalesSummary
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class ProductSalesInfo
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategorySalesInfo
    {
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class InventorySummary
    {
        public int TotalProducts { get; set; }
        public int TotalStockUnits { get; set; }
        public decimal TotalStockValue { get; set; } // Based on cost
    }

    public class ProductStockInfo
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
    }

    public class StockMovementSummary
    {
        public int TotalMovements { get; set; }
    }

    public class StockMovementInfo
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public int QuantityChanged { get; set; }
        public DateTime MovementDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class DetailedSalesSummary
    {
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalItemsSold { get; set; }
    }

    public class DetailedSaleInfo
    {
        public string SaleId { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<DetailedSaleItemInfo> SaleItems { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount => TotalAmount - Discount;
    }

    public class DetailedSaleItemInfo
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // Stock Movement Report Models
    public class StockMovementReportResponse
    {
        public GeneralReportInfo GeneralInfo { get; set; } = new();
        public StockMovementSummary StockMovementSummary { get; set; } = new();
        public List<StockMovementInfo> StockMovements { get; set; } = new();
    }

    // Detailed Sales Report Models
    public class DetailedSalesReportResponse
    {
        public GeneralReportInfo GeneralInfo { get; set; } = new();
        public DetailedSalesSummary DetailedSalesSummary { get; set; } = new();
        public List<DetailedSaleInfo> Sales { get; set; } = new();
    }
}