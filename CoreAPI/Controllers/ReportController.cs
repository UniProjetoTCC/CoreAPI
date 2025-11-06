using Business.DataRepositories;
using Business.Enums;
using Business.Models;
using Business.Services.Base;
using CoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILinkedUserService _linkedUserService;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IProductRepository _productRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IPromotionRepository _promotionRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IStockMovementRepository _stockMovementRepository;


        public ReportController(
            UserManager<IdentityUser> userManager,
            ILinkedUserService linkedUserService,
            ILinkedUserRepository linkedUserRepository,
            IUserGroupRepository userGroupRepository,
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            ISaleRepository saleRepository,
            IStockRepository stockRepository,
            IPromotionRepository promotionRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IStockMovementRepository stockMovementRepository)
        {
            _userManager = userManager;
            _linkedUserService = linkedUserService;
            _linkedUserRepository = linkedUserRepository;
            _categoryRepository = categoryRepository;
            _userGroupRepository = userGroupRepository;
            _productRepository = productRepository;
            _saleRepository = saleRepository;
            _stockRepository = stockRepository;
            _promotionRepository = promotionRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _stockMovementRepository = stockMovementRepository;
        }

        /// <summary>
        /// Generates a sales report for a given date range.
        /// </summary>
        /// <param name="startDate">Start date for the report (yyyy-mm-dd)</param>
        /// <param name="endDate">End date for the report (yyyy-mm-dd)</param>
        /// <response code="200">Sales report data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet("Sales")]
        public async Task<ActionResult<SalesReportResponse>> GetSalesReport(DateTime startDate, DateTime endDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckReportsPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var utcStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            var utcEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            var (sales, _, totalAmount) = await _saleRepository.SearchSalesAsync(
                groupId, utcStartDate, utcEndDate, null, null, null, 1, int.MaxValue);

            var saleItems = sales.SelectMany(s => s.SaleItems).ToList();

            var (allCategories, _) = await _categoryRepository.SearchByNameAsync("", groupId, 1, int.MaxValue);
            var categoryDictionary = allCategories.ToDictionary(c => c.Id, c => c.Name);

            var salesSummary = new SalesSummary
            {
                TotalOrders = sales.Count,
                TotalRevenue = totalAmount,
                AverageOrderValue = sales.Any() ? totalAmount / sales.Count : 0
            };

            var report = new SalesReportResponse
            {
                GeneralInfo = new GeneralReportInfo { ReportName = "Sales Report", GeneratedAt = DateTime.UtcNow, ReportPeriod = new DateRange { StartDate = startDate, EndDate = endDate } },
                SalesSummary = salesSummary,
                TopSellingProducts = saleItems
                    .Where(si => si.Product != null)
                    .GroupBy(si => si.Product!.Id)
                    .Select(g => new ProductSalesInfo
                    {
                        ProductId = g.Key,
                        ProductName = g.First().Product!.Name,
                        UnitsSold = g.Sum(si => si.Quantity),
                        Revenue = g.Sum(si => si.Quantity * si.UnitPrice)
                    })
                    .OrderByDescending(p => p.Revenue).Take(10).ToList(),
                TopSellingCategories = saleItems
                    .Where(si => si.Product != null)
                    .GroupBy(si => si.Product!.CategoryId)
                    .Select(g => new CategorySalesInfo
                    {
                        CategoryId = g.Key,
                        CategoryName = categoryDictionary.GetValueOrDefault(g.Key, "No Category"),
                        UnitsSold = g.Sum(si => si.Quantity),
                        Revenue = g.Sum(si => si.Quantity * si.UnitPrice)
                    })
                    .OrderByDescending(c => c.Revenue).Take(5).ToList()
            };

            return Ok(report);
        }

        /// <summary>
        /// Generates an inventory report with stock levels.
        /// </summary>
        /// <param name="lowStockThreshold">Threshold to identify products with low stock. Default is 10.</param>
        /// <param name="highStockThreshold">Threshold to identify products with high stock. Default is 100.</param>
        /// <response code="200">Inventory report data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet("Inventory")]
        public async Task<ActionResult<InventoryReportResponse>> GetInventoryReport([FromQuery] int lowStockThreshold = 10, [FromQuery] int highStockThreshold = 100)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckReportsPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var (allProducts, _) = await _productRepository.SearchByNameAsync("", groupId, 1, int.MaxValue);

            var allStocks = new List<StockBusinessModel>();
            foreach (var product in allProducts)
            {
                var stock = await _stockRepository.GetByProductIdAsync(product.Id, groupId);
                if (stock != null)
                {
                    allStocks.Add(stock);
                }
            }

            var validStocks = allStocks.Where(s => s.Product != null).ToList();

            var inventorySummary = new InventorySummary
            {
                TotalProducts = validStocks.Count,
                TotalStockUnits = validStocks.Sum(s => s.Quantity),
                TotalStockValue = validStocks.Sum(s => s.Quantity * s.Product!.Cost)
            };

            var report = new InventoryReportResponse
            {
                GeneralInfo = new GeneralReportInfo { ReportName = "Inventory Report", GeneratedAt = DateTime.UtcNow },
                InventorySummary = inventorySummary,
                ProductsWithLowStock = validStocks
                    .Where(s => s.Quantity <= lowStockThreshold)
                    .Select(s => new ProductStockInfo { ProductId = s.Product!.Id, ProductName = s.Product.Name, SKU = s.Product.SKU, StockQuantity = s.Quantity, Cost = s.Product.Cost, Price = s.Product.Price })
                    .OrderBy(p => p.StockQuantity).ToList(),
                ProductsWithHighStock = validStocks
                    .Where(s => s.Quantity >= highStockThreshold)
                    .Select(s => new ProductStockInfo { ProductId = s.Product!.Id, ProductName = s.Product.Name, SKU = s.Product.SKU, StockQuantity = s.Quantity, Cost = s.Product.Cost, Price = s.Product.Price })
                    .OrderByDescending(p => p.StockQuantity).ToList()
            };

            return Ok(report);
        }

        /// <summary>
        /// Generates a stock movement report for a given date range.
        /// </summary>
        /// <param name="startDate">Start date for the report (yyyy-mm-dd)</param>
        /// <param name="endDate">End date for the report (yyyy-mm-dd)</param>
        /// <response code="200">Stock movement report data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet("StockMovement")]
        public async Task<ActionResult<StockMovementReportResponse>> GetStockMovementReport(DateTime startDate, DateTime endDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckReportsPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var utcStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            var utcEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            var movements = await _stockMovementRepository.GetAllByDateRangeAndGroupIdAsync(utcStartDate, utcEndDate, groupId);

            var stockIds = movements.Select(m => m.StockId).Distinct();

            var stocks = await _stockRepository.GetByIdsAsync(stockIds, groupId);
            var stocksDict = stocks.ToDictionary(s => s.Id);

            var productIds = stocks.Select(s => s.ProductId).Distinct();

            var products = await _productRepository.GetByIdsAsync(productIds, groupId);
            var productsDict = products.ToDictionary(p => p.Id);

            var movementInfos = movements.Select(m =>
            {
                stocksDict.TryGetValue(m.StockId, out var stock);
                productsDict.TryGetValue(stock?.ProductId ?? string.Empty, out var product);

                return new StockMovementInfo
                {
                    ProductId = stock?.ProductId ?? "N/A",
                    ProductName = product?.Name ?? "N/A",
                    SKU = product?.SKU ?? "N/A",
                    MovementType = m.MovementType.ToString(),
                    QuantityChanged = m.Quantity,
                    MovementDate = m.MovementDate,
                    Reason = m.Reason?.ToString() ?? "N/A",
                };
            }).ToList();

            var report = new StockMovementReportResponse
            {
                GeneralInfo = new GeneralReportInfo { ReportName = "Stock Movement Report", GeneratedAt = DateTime.UtcNow, ReportPeriod = new DateRange { StartDate = startDate, EndDate = endDate } },
                StockMovementSummary = new StockMovementSummary
                {
                    TotalMovements = movements.Count(),
                },
                StockMovements = movementInfos
            };

            return Ok(report);
        }

        /// <summary>
        /// Generates a detailed sales report for a given date range.
        /// </summary>
        /// <param name="startDate">Start date for the report (yyyy-mm-dd)</param>
        /// <param name="endDate">End date for the report (yyyy-mm-dd)</param>
        /// <response code="200">Detailed sales report data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet("DetailedSales")]
        public async Task<ActionResult<DetailedSalesReportResponse>> GetDetailedSalesReport(DateTime startDate, DateTime endDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckReportsPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var utcStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            var utcEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            var (sales, _, totalAmount) = await _saleRepository.SearchSalesAsync(
                groupId, utcStartDate, utcEndDate, null, null, null, 1, int.MaxValue);

            var promotionIds = sales
                .SelectMany(s => s.SaleItems)
                .Where(si => !string.IsNullOrEmpty(si.PromotionId))
                .Select(si => si.PromotionId!)
                .Distinct()
                .ToList();

            var promotions = new List<PromotionBusinessModel>();
            if (promotionIds.Any())
            {
                promotions = await _promotionRepository.GetByIdsAsync(promotionIds, groupId);
            }
            var promotionsDict = promotions.ToDictionary(p => p.Id);

            var detailedSales = sales.Select(s =>
            {
                decimal totalDiscountForSale = 0;

                var saleItemsInfo = s.SaleItems.Select(si =>
                {
                    var itemTotalPrice = si.Quantity * si.UnitPrice;
                    var itemDiscount = 0m;

                    // Calcula o desconto para este item específico
                    if (!string.IsNullOrEmpty(si.PromotionId) && promotionsDict.TryGetValue(si.PromotionId, out var promotion))
                    {
                        if (promotion.DiscountPercentage > 0)
                        {
                            itemDiscount = itemTotalPrice * (promotion.DiscountPercentage / 100m);
                            totalDiscountForSale += itemDiscount; // Acumula o desconto no total da venda
                        }
                    }

                    return new DetailedSaleItemInfo
                    {
                        ProductId = si.Product?.Id ?? string.Empty,
                        ProductName = si.Product?.Name ?? "N/A",
                        Quantity = si.Quantity,
                        UnitPrice = si.UnitPrice,
                        TotalPrice = itemTotalPrice,
                        // Opcional: Adicionar um campo de desconto ao item, se quiser
                        // Discount = itemDiscount 
                    };
                }).ToList();

                return new DetailedSaleInfo
                {
                    SaleId = s.Id,
                    SaleDate = s.SaleDate,
                    CustomerName = s.Customer?.Name ?? "N/A",
                    TotalAmount = s.SaleItems.Sum(item => item.Quantity * item.UnitPrice),
                    Discount = totalDiscountForSale, // O desconto total da venda é a soma dos descontos dos itens
                    SaleItems = saleItemsInfo
                };
            }).ToList();

            var report = new DetailedSalesReportResponse
            {
                GeneralInfo = new GeneralReportInfo { ReportName = "Detailed Sales Report", GeneratedAt = DateTime.UtcNow, ReportPeriod = new DateRange { StartDate = startDate, EndDate = endDate } },
                DetailedSalesSummary = new DetailedSalesSummary
                {
                    TotalSales = sales.Count(),
                    TotalRevenue = totalAmount, // Este valor pode precisar de ajuste se já considera descontos
                    TotalItemsSold = sales.SelectMany(s => s.SaleItems).Sum(si => si.Quantity)
                },
                Sales = detailedSales
            };

            return Ok(report);
        }

        /// <summary>
        /// Generates a purchase order report for a given date range.
        /// </summary>
        /// <param name="startDate">Start date for the report (yyyy-mm-dd)</param>
        /// <param name="endDate">End date for the report (yyyy-mm-dd)</param>
        /// <param name="supplierId">Optional supplier ID to filter</param>
        /// <param name="status">Optional status to filter (Pending, Completed, Cancelled)</param>
        /// <response code="200">Purchase order report data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet("PurchaseOrders")]
        public async Task<ActionResult<PurchaseOrderReportResponse>> GetPurchaseOrderReport(
            DateTime startDate,
            DateTime endDate,
            [FromQuery] string? supplierId = null,
            [FromQuery] string? status = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckReportsPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var utcStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            var utcEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            var (orders, totalCount) = await _purchaseOrderRepository.SearchAsync(
                groupId, utcStartDate, utcEndDate, supplierId, status, 1, int.MaxValue);

            var report = new PurchaseOrderReportResponse
            {
                GeneralInfo = new GeneralReportInfo
                {
                    ReportName = "Purchase Order Report",
                    GeneratedAt = DateTime.UtcNow,
                    ReportPeriod = new DateRange { StartDate = startDate, EndDate = endDate }
                },
                PurchaseOrderSummary = new PurchaseOrderSummary
                {
                    TotalOrders = totalCount,
                    TotalValue = orders.Sum(o => o.TotalAmount),
                    PendingOrders = orders.Count(o => o.Status == "Pending"),
                    CompletedOrders = orders.Count(o => o.Status == "Completed"),
                    CancelledOrders = orders.Count(o => o.Status == "Cancelled")
                },
                PurchaseOrders = orders.Select(o => new PurchaseOrderInfo
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    SupplierName = o.Supplier?.Name ?? "N/A",
                    ItemCount = o.Items.Count,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status ?? "N/A",
                    DeliveryDate = o.DeliveryDate
                }).ToList()
            };

            return Ok(report);
        }

        private async Task<(bool hasPermission, string groupId)> CheckReportsPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Report);

                if (!permission)
                {
                    return (false, string.Empty);
                }

                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
                groupId = linkedUser?.GroupId ?? string.Empty;
            }
            else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(userId);
                groupId = group?.GroupId ?? string.Empty;
            }

            return (true, groupId);
        }
    }
}