using AutoMapper;
using Business.DataRepositories;
using Business.Enums;
using Business.Services.Base;
using CoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace CoreAPI.Controllers
{
    /// <summary>
    /// Controller responsible for managing stock.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class StockController : ControllerBase
    {
        private readonly ILinkedUserService _linkedUserService;
        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<StockController> _logger;

        public StockController(
            ILinkedUserService linkedUserService,
            IStockRepository stockRepository,
            IProductRepository productRepository,
            UserManager<IdentityUser> userManager,
            IUserGroupRepository userGroupRepository,
            ILinkedUserRepository linkedUserRepository,
            IMapper mapper,
            IDistributedCache distributedCache,
            ILogger<StockController> logger)
        {
            _linkedUserService = linkedUserService;
            _stockRepository = stockRepository;
            _productRepository = productRepository;
            _userManager = userManager;
            _userGroupRepository = userGroupRepository;
            _linkedUserRepository = linkedUserRepository;
            _mapper = mapper;
            _distributedCache = distributedCache;
            _logger = logger;
        }

        /// <summary>
        /// Gets current stock level for a product
        /// </summary>
        /// <remarks>
        /// Retrieves the current stock quantity for a specific product.
        /// The product must belong to the user's group.
        /// </remarks>
        /// <param name="productId">Product ID to check stock for</param>
        /// <response code="200">Stock information</response>
        /// <response code="400">Invalid product ID</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Product not found</response>
        [HttpGet("{productId}")]
        [Authorize]
        public async Task<ActionResult> GetProductStock(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return BadRequest("Product ID is required");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckStockPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to manage stock. Talk to your access manager to get the necessary permissions.");
            }

            if (string.IsNullOrEmpty(groupId))
                return BadRequest("User group not found");

            // Check if product exists and belongs to the user's group
            var product = await _productRepository.GetById(productId, groupId);
            if (product == null)
                return NotFound("Product not found or does not belong to your group");

            // Get stock information
            var stock = await _stockRepository.GetByProductIdAsync(productId, groupId);
            if (stock == null)
            {
                // No stock record yet, return 0
                return Ok(new StockDto
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    Quantity = 0,
                    CreatedAt = null
                });
            }

            var stockDto = new StockDto
            {
                Id = stock.Id,
                ProductId = stock.ProductId,
                ProductName = product.Name,
                Quantity = stock.Quantity,
                CreatedAt = stock.CreatedAt,
                UpdatedAt = stock.UpdatedAt
            };

            return Ok(stockDto);
        }

        /// <summary>
        /// Updates the stock level for a product
        /// </summary>
        /// <remarks>
        /// Sets the stock to the exact quantity specified.
        /// This will override any existing stock level, so use with caution.
        /// For relative adjustments, use the Add or Deduct endpoints instead.
        /// </remarks>
        /// <param name="model">Stock update model with product ID and new quantity</param>
        /// <response code="200">Updated stock information</response>
        /// <response code="400">Invalid input or product not found</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpPut]
        [Authorize]
        public async Task<ActionResult> UpdateStock([FromBody] StockUpdateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckStockPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to manage stock. Talk to your access manager to get the necessary permissions.");
            }

            if (string.IsNullOrEmpty(groupId))
                return BadRequest("User group not found");

            // Check if product exists and belongs to the user's group
            var product = await _productRepository.GetById(model.ProductId, groupId);
            if (product == null)
                return BadRequest("Product not found or does not belong to your group");

            // Update stock
            var stock = await _stockRepository.UpdateStockAsync(model.ProductId, groupId, (int)model.Quantity, currentUser.Id, model.Reason);
            if (stock == null)
                return BadRequest("Failed to update stock");

            var stockDto = new StockDto
            {
                Id = stock.Id,
                ProductId = stock.ProductId,
                ProductName = product.Name,
                Quantity = stock.Quantity,
                CreatedAt = stock.CreatedAt,
                UpdatedAt = stock.UpdatedAt
            };

            return Ok(stockDto);
        }

        /// <summary>
        /// Adds quantity to product stock
        /// </summary>
        /// <remarks>
        /// Increases the stock level by the specified quantity.
        /// If no stock record exists for the product, one will be created.
        /// </remarks>
        /// <param name="model">Stock adjustment model with product ID and quantity to add</param>
        /// <response code="200">Updated stock information</response>
        /// <response code="400">Invalid input or product not found</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpPost("Add")]
        [Authorize]
        public async Task<ActionResult> AddStock([FromBody] StockAdjustmentModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckStockPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to manage stock. Talk to your access manager to get the necessary permissions.");
            }

            if (string.IsNullOrEmpty(groupId))
                return BadRequest("User group not found");

            // Check if product exists and belongs to the user's group
            var product = await _productRepository.GetById(model.ProductId, groupId);
            if (product == null)
                return BadRequest("Product not found or does not belong to your group");

            // Add stock
            var stock = await _stockRepository.AddStockAsync(model.ProductId, groupId, (int)model.Quantity, currentUser.Id, model.Reason);
            if (stock == null)
                return BadRequest("Failed to add stock");

            var stockDto = new StockDto
            {
                Id = stock.Id,
                ProductId = stock.ProductId,
                ProductName = product.Name,
                Quantity = stock.Quantity,
                CreatedAt = stock.CreatedAt,
                UpdatedAt = stock.UpdatedAt
            };

            return Ok(stockDto);
        }

        /// <summary>
        /// Deducts quantity from product stock
        /// </summary>
        /// <remarks>
        /// Decreases the stock level by the specified quantity.
        /// Will return an error if there is insufficient stock.
        /// </remarks>
        /// <param name="model">Stock deduction model with product ID and quantity to deduct</param>
        /// <response code="200">Updated stock information</response>
        /// <response code="400">Invalid input, product not found, or insufficient stock</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpPost("Deduct")]
        [Authorize]
        public async Task<ActionResult> DeductStock([FromBody] StockDeductionModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckStockPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to manage stock. Talk to your access manager to get the necessary permissions.");
            }

            if (string.IsNullOrEmpty(groupId))
                return BadRequest("User group not found");

            // Check if product exists and belongs to the user's group
            var product = await _productRepository.GetById(model.ProductId, groupId);
            if (product == null)
                return BadRequest("Product not found or does not belong to your group");

            // Check if there's enough stock
            bool hasStock = await _stockRepository.HasStockAsync(model.ProductId, groupId, (int)model.Quantity);
            if (!hasStock)
                return BadRequest("Insufficient stock available");

            // Deduct stock
            var stock = await _stockRepository.DeductStockAsync(model.ProductId, groupId, (int)model.Quantity, currentUser.Id, model.Reason);
            if (stock == null)
                return BadRequest("Failed to deduct stock");

            var stockDto = new StockDto
            {
                Id = stock.Id,
                ProductId = stock.ProductId,
                ProductName = product.Name,
                Quantity = stock.Quantity,
                CreatedAt = stock.CreatedAt,
                UpdatedAt = stock.UpdatedAt
            };

            return Ok(stockDto);
        }

        /// <summary>
        /// Gets products with low stock
        /// </summary>
        /// <remarks>
        /// Retrieves all products with stock at or below the specified threshold.
        /// Default threshold is 10 units if not specified.
        /// Results are cached for 1 minute to improve performance.
        /// </remarks>
        /// <param name="threshold">Optional stock threshold (default: 10)</param>
        /// <response code="200">List of products with low stock</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet("LowStock")]
        [Authorize]
        public async Task<ActionResult> GetLowStockProducts([FromQuery] decimal threshold = 10)
        {
            const int cacheDurationMinutes = 1;
            const int maxCachedQueriesPerUser = 5;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckStockPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to manage stock. Talk to your access manager to get the necessary permissions.");
            }

            if (string.IsNullOrEmpty(groupId))
                return BadRequest("User group not found");

            // Generate cache key for this query
            string cacheKey = $"lowstock:{currentUser.Id}:{groupId}:{threshold}";
            string queriesMapKey = $"lowstock_queries:{currentUser.Id}:{groupId}";

            try
            {
                // Try to get result directly from cache first
                var cachedResultBytes = await _distributedCache.GetAsync(cacheKey);
                if (cachedResultBytes != null)
                {
                    // We have a direct cache hit
                    var cachedResultJson = Encoding.UTF8.GetString(cachedResultBytes);
                    var cachedData = JsonSerializer.Deserialize<List<StockDto>>(
                        cachedResultJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Ok(cachedData);
                }

                // No direct cache hit, try to get previously cached queries
                Dictionary<int, CachedStockData>? userCachedQueries = null;
                var cachedQueriesBytes = await _distributedCache.GetAsync(queriesMapKey);

                if (cachedQueriesBytes != null)
                {
                    // Deserialize the map of cached queries
                    string cachedQueriesJson = Encoding.UTF8.GetString(cachedQueriesBytes);
                    userCachedQueries = JsonSerializer.Deserialize<Dictionary<int, CachedStockData>>(
                        cachedQueriesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                // Initialize if null
                userCachedQueries ??= new Dictionary<int, CachedStockData>();

                // Get low stock products from database
                var lowStockItems = await _stockRepository.GetLowStockProductsAsync(groupId, (int)threshold);

                var lowStockDtos = new List<StockDto>();
                foreach (var item in lowStockItems)
                {
                    if (item.Product != null)
                    {
                        lowStockDtos.Add(new StockDto
                        {
                            Id = item.Id,
                            ProductId = item.ProductId,
                            ProductName = item.Product.Name,
                            Quantity = item.Quantity,
                            CreatedAt = item.CreatedAt,
                            UpdatedAt = item.UpdatedAt
                        });
                    }
                }

                // Store in cache
                userCachedQueries[(int)threshold] = new CachedStockData
                {
                    LowStockItems = lowStockDtos,
                    Timestamp = DateTime.UtcNow
                };

                // Ensure we don't exceed the cache limit per user
                EnsureStockCacheLimitNotExceeded(userCachedQueries, maxCachedQueriesPerUser);

                // Store the updated cache
                var serializedQueries = JsonSerializer.Serialize(userCachedQueries);
                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));

                await _distributedCache.SetAsync(
                    queriesMapKey,
                    Encoding.UTF8.GetBytes(serializedQueries),
                    cacheOptions);

                // Also store the direct result for faster retrieval next time
                var serializedResult = JsonSerializer.Serialize(lowStockDtos);
                await _distributedCache.SetAsync(
                    cacheKey,
                    Encoding.UTF8.GetBytes(serializedResult),
                    cacheOptions);

                return Ok(lowStockDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock products");

                // Fall back to database query if caching fails
                var lowStockItems = await _stockRepository.GetLowStockProductsAsync(groupId, (int)threshold);

                var lowStockDtos = new List<StockDto>();
                foreach (var item in lowStockItems)
                {
                    if (item.Product != null)
                    {
                        lowStockDtos.Add(new StockDto
                        {
                            Id = item.Id,
                            ProductId = item.ProductId,
                            ProductName = item.Product.Name,
                            Quantity = item.Quantity,
                            CreatedAt = item.CreatedAt,
                            UpdatedAt = item.UpdatedAt
                        });
                    }
                }

                return Ok(lowStockDtos);
            }
        }

        /// <summary>
        /// Checks if a product has sufficient stock
        /// </summary>
        /// <remarks>
        /// Verifies if the product has enough stock for the requested quantity.
        /// Useful for pre-checking before attempting operations that require stock.
        /// </remarks>
        /// <param name="productId">Product ID to check</param>
        /// <param name="quantity">Required quantity</param>
        /// <response code="200">Boolean indicating if sufficient stock is available</response>
        /// <response code="400">Invalid input or product not found</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("Check/{productId}")]
        [Authorize]
        public async Task<ActionResult> CheckStock(string productId, [FromQuery] decimal quantity)
        {
            if (string.IsNullOrEmpty(productId))
                return BadRequest("Product ID is required");

            if (quantity <= 0)
                return BadRequest("Quantity must be greater than zero");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckStockPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to manage stock. Talk to your access manager to get the necessary permissions.");
            }

            if (string.IsNullOrEmpty(groupId))
                return BadRequest("User group not found");

            // Check if product exists and belongs to the user's group
            var product = await _productRepository.GetById(productId, groupId);
            if (product == null)
                return BadRequest("Product not found or does not belong to your group");

            // Check if there's enough stock
            bool hasStock = await _stockRepository.HasStockAsync(productId, groupId, (int)quantity);

            return Ok(new { HasSufficientStock = hasStock });
        }

        // Helper method to limit the number of cache entries per user
        private void EnsureStockCacheLimitNotExceeded(Dictionary<int, CachedStockData> cache, int maxSize)
        {
            if (cache.Count <= maxSize)
                return;

            // Calculate how many items to remove
            int itemsToRemove = cache.Count - maxSize;

            // Get the oldest entries based on timestamp
            var oldestEntries = cache
                .OrderBy(kv => kv.Value.Timestamp)
                .Take(itemsToRemove)
                .Select(kv => kv.Key)
                .ToList();

            // Remove the oldest entries
            foreach (var key in oldestEntries)
            {
                cache.Remove(key);
            }
        }

        // Helper method to get user's group ID
        private async Task<string> GetUserGroupId(string userId)
        {
            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
                return linkedUser?.GroupId ?? string.Empty;
            }
            else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(userId);
                return group?.GroupId ?? string.Empty;
            }
        }

        private async Task<(bool hasPermission, string groupId)> CheckStockPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Stock);

                if (!permission)
                {
                    return (false, string.Empty);
                }

                // Get the group ID from the linked user
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
