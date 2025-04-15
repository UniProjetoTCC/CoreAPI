using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using AutoMapper;
using Business.DataRepositories;
using Business.Services.Base;
using Business.Enums;
using CoreAPI.Models;
using Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StockMovementController : ControllerBase
    {
        private readonly ILinkedUserService _linkedUserService;
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IDistributedCache _distributedCache;
        private readonly IMapper _mapper;
        private readonly ILogger<StockMovementController> _logger;

        public StockMovementController(
            ILinkedUserService linkedUserService,
            IStockMovementRepository stockMovementRepository,
            IStockRepository stockRepository,
            IProductRepository productRepository,
            UserManager<IdentityUser> userManager,
            IUserGroupRepository userGroupRepository,
            ILinkedUserRepository linkedUserRepository,
            IDistributedCache distributedCache,
            IMapper mapper,
            ILogger<StockMovementController> logger)
        {
            _linkedUserService = linkedUserService;
            _stockMovementRepository = stockMovementRepository;
            _stockRepository = stockRepository;
            _productRepository = productRepository;
            _userManager = userManager;
            _userGroupRepository = userGroupRepository;
            _linkedUserRepository = linkedUserRepository;
            _distributedCache = distributedCache;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Gets stock movement history for a product
        /// </summary>
        /// <remarks>
        /// Retrieves the stock movement history for a specific product with pagination.
        /// The product must belong to the user's group.
        /// Results are cached for 1 minute to improve performance.
        /// </remarks>
        /// <param name="productId">Product ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 20)</param>
        /// <response code="200">Stock movement history with pagination info</response>
        /// <response code="400">Invalid product ID</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Product not found</response>
        [HttpGet("Product/{productId}")]
        [Authorize]
        public async Task<ActionResult> GetProductMovements(string productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            const int cacheDurationMinutes = 1;
            const int maxCachedQueriesPerUser = 5;

            if (string.IsNullOrEmpty(productId))
                return BadRequest("Product ID is required");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            string groupId = await GetUserGroupId(currentUser.Id);
            if (string.IsNullOrEmpty(groupId))
                return BadRequest("User group not found");

            // Check if product exists and belongs to the user's group
            var product = await _productRepository.GetById(productId, groupId);
            if (product == null)
                return NotFound("Product not found or does not belong to your group");

            // Generate cache key for this query
            string cacheKey = $"stockmovements:product:{currentUser.Id}:{groupId}:{productId}:{page}:{pageSize}";
            string queriesMapKey = $"stockmovements_product_queries:{currentUser.Id}:{groupId}";

            try
            {
                // Try to get result directly from cache first
                var cachedResultBytes = await _distributedCache.GetAsync(cacheKey);
                if (cachedResultBytes != null)
                {
                    // We have a direct cache hit
                    var cachedResultJson = Encoding.UTF8.GetString(cachedResultBytes);
                    var cachedData = JsonSerializer.Deserialize<StockMovementSearchResponse>(
                        cachedResultJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return Ok(cachedData);
                }

                // No direct cache hit, try to get the map of cached queries
                Dictionary<string, CachedStockMovementData>? userCachedQueries = null;
                var cachedQueriesBytes = await _distributedCache.GetAsync(queriesMapKey);
                
                if (cachedQueriesBytes != null)
                {
                    // Deserialize the map of cached queries
                    var cachedQueriesJson = Encoding.UTF8.GetString(cachedQueriesBytes);
                    userCachedQueries = JsonSerializer.Deserialize<Dictionary<string, CachedStockMovementData>?>(
                        cachedQueriesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                // Initialize if null
                userCachedQueries ??= new Dictionary<string, CachedStockMovementData>();

                // No cache hit, get data from database
                var movements = await _stockMovementRepository.GetByProductIdAsync(productId, groupId, page, pageSize);
                var totalItems = await _stockMovementRepository.GetTotalMovementsForProductAsync(productId, groupId);
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var movementDtos = movements.Select(m => _mapper.Map<StockMovementDto>(m)).ToList();
                foreach (var dto in movementDtos)
                {
                    if (string.IsNullOrEmpty(dto.ProductName))
                    {
                        dto.ProductName = product.Name;
                    }
                }

                var response = new StockMovementSearchResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = movementDtos
                };

                // Store in cache map
                var cacheKeyIdentifier = $"{productId}:{page}:{pageSize}";
                userCachedQueries[cacheKeyIdentifier] = new CachedStockMovementData
                {
                    Movements = movementDtos,
                    Timestamp = DateTime.UtcNow
                };

                // Ensure we don't exceed the cache limit per user
                EnsureMovementsCacheLimitNotExceeded(userCachedQueries, maxCachedQueriesPerUser);

                // Store the updated cache map
                var serializedQueries = JsonSerializer.Serialize(userCachedQueries);
                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));

                await _distributedCache.SetAsync(
                    queriesMapKey,
                    Encoding.UTF8.GetBytes(serializedQueries),
                    cacheOptions);

                // Also store the direct result for faster retrieval next time
                var serializedResult = JsonSerializer.Serialize(response);
                await _distributedCache.SetAsync(
                    cacheKey,
                    Encoding.UTF8.GetBytes(serializedResult),
                    cacheOptions);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock movements for product {ProductId}", productId);
                
                // Fall back to database query if caching fails
                var movements = await _stockMovementRepository.GetByProductIdAsync(productId, groupId, page, pageSize);
                var totalItems = await _stockMovementRepository.GetTotalMovementsForProductAsync(productId, groupId);
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var movementDtos = movements.Select(m => _mapper.Map<StockMovementDto>(m)).ToList();
                foreach (var dto in movementDtos)
                {
                    if (string.IsNullOrEmpty(dto.ProductName))
                    {
                        dto.ProductName = product.Name;
                    }
                }

                var response = new StockMovementSearchResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = movementDtos
                };

                return Ok(response);
            }
        }

        /// <summary>
        /// Gets stock movement history for a specific stock entry
        /// </summary>
        /// <remarks>
        /// Retrieves the stock movement history for a specific stock entry with pagination.
        /// The stock entry must belong to the user's group.
        /// Results are cached for 1 minute to improve performance.
        /// </remarks>
        /// <param name="stockId">Stock ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 20)</param>
        /// <response code="200">Stock movement history with pagination info</response>
        /// <response code="400">Invalid stock ID</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Stock not found</response>
        [HttpGet("Stock/{stockId}")]
        [Authorize]
        public async Task<ActionResult> GetStockMovements(string stockId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            const int cacheDurationMinutes = 1;
            const int maxCachedQueriesPerUser = 5;

            if (string.IsNullOrEmpty(stockId))
                return BadRequest("Stock ID is required");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            string groupId = await GetUserGroupId(currentUser.Id);
            if (string.IsNullOrEmpty(groupId))
                return BadRequest("User group not found");

            // Generate cache key for this query
            string cacheKey = $"stockmovements:stock:{currentUser.Id}:{groupId}:{stockId}:{page}:{pageSize}";
            string queriesMapKey = $"stockmovements_stock_queries:{currentUser.Id}:{groupId}";

            try
            {
                // Try to get result directly from cache first
                var cachedResultBytes = await _distributedCache.GetAsync(cacheKey);
                if (cachedResultBytes != null)
                {
                    // We have a direct cache hit
                    var cachedResultJson = Encoding.UTF8.GetString(cachedResultBytes);
                    var cachedData = JsonSerializer.Deserialize<StockMovementSearchResponse>(
                        cachedResultJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return Ok(cachedData);
                }

                // No direct cache hit, try to get the map of cached queries
                Dictionary<string, CachedStockMovementData>? userCachedQueries = null;
                var cachedQueriesBytes = await _distributedCache.GetAsync(queriesMapKey);
                
                if (cachedQueriesBytes != null)
                {
                    // Deserialize the map of cached queries
                    var cachedQueriesJson = Encoding.UTF8.GetString(cachedQueriesBytes);
                    userCachedQueries = JsonSerializer.Deserialize<Dictionary<string, CachedStockMovementData>?>(
                        cachedQueriesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                // Initialize if null
                userCachedQueries ??= new Dictionary<string, CachedStockMovementData>();

                // No cache hit, get data from database
                var movements = await _stockMovementRepository.GetByStockIdAsync(stockId, page, pageSize);
                
                if (movements.Count == 0)
                    return NotFound("No stock movements found");

                // Verify that the stock belongs to the user's group
                var firstMovement = movements.FirstOrDefault();
                if (firstMovement?.Stock?.GroupId != groupId)
                    return BadRequest("Stock does not belong to your group");

                var movementDtos = movements.Select(m => _mapper.Map<StockMovementDto>(m)).ToList();

                // Get the stock item to count total movements (instead of direct database access)
                var stock = await _stockRepository.GetByProductIdAsync(firstMovement.Stock.ProductId, groupId);
                
                // Use the repository method to get count
                var totalItems = movements.Count > 0 
                    ? await _stockMovementRepository.GetTotalMovementsForProductAsync(firstMovement.Stock.ProductId, groupId) 
                    : 0;
                    
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var response = new StockMovementSearchResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = movementDtos
                };

                // Store in cache map
                var cacheKeyIdentifier = $"{stockId}:{page}:{pageSize}";
                userCachedQueries[cacheKeyIdentifier] = new CachedStockMovementData
                {
                    Movements = movementDtos,
                    Timestamp = DateTime.UtcNow
                };

                // Ensure we don't exceed the cache limit per user
                EnsureMovementsCacheLimitNotExceeded(userCachedQueries, maxCachedQueriesPerUser);

                // Store the updated cache map
                var serializedQueries = JsonSerializer.Serialize(userCachedQueries);
                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));

                await _distributedCache.SetAsync(
                    queriesMapKey,
                    Encoding.UTF8.GetBytes(serializedQueries),
                    cacheOptions);

                // Also store the direct result for faster retrieval next time
                var serializedResult = JsonSerializer.Serialize(response);
                await _distributedCache.SetAsync(
                    cacheKey,
                    Encoding.UTF8.GetBytes(serializedResult),
                    cacheOptions);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock movements for stock {StockId}", stockId);
                
                // Fall back to database query if caching fails
                var movements = await _stockMovementRepository.GetByStockIdAsync(stockId, page, pageSize);
                
                if (movements.Count == 0)
                    return NotFound("No stock movements found");

                // Verify that the stock belongs to the user's group
                var firstMovement = movements.FirstOrDefault();
                if (firstMovement?.Stock?.GroupId != groupId)
                    return BadRequest("Stock does not belong to your group");

                var movementDtos = movements.Select(m => _mapper.Map<StockMovementDto>(m)).ToList();

                // Use the repository method to get count
                var totalItems = movements.Count > 0 
                    ? await _stockMovementRepository.GetTotalMovementsForProductAsync(firstMovement.Stock.ProductId, groupId) 
                    : 0;
                    
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var response = new StockMovementSearchResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = movementDtos
                };

                return Ok(response);
            }
        }

        /// <summary>
        /// Gets specific stock movement details
        /// </summary>
        /// <remarks>
        /// Retrieves detailed information about a specific stock movement.
        /// The movement must be related to a product that belongs to the user's group.
        /// </remarks>
        /// <param name="id">Stock movement ID</param>
        /// <response code="200">Stock movement details</response>
        /// <response code="400">Invalid movement ID</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Movement not found</response>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> GetMovement(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Movement ID is required");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            string groupId = await GetUserGroupId(currentUser.Id);
            if (string.IsNullOrEmpty(groupId))
                return BadRequest("User group not found");

            // Get stock movement
            var movement = await _stockMovementRepository.GetByIdAsync(id);
            if (movement == null)
                return NotFound("Stock movement not found");

            // Verify that the stock belongs to the user's group
            if (movement.Stock?.GroupId != groupId)
                return BadRequest("Stock movement does not belong to your group");

            var movementDto = _mapper.Map<StockMovementDto>(movement);
            return Ok(movementDto);
        }

        // Helper method to limit the number of cache entries per user
        private void EnsureMovementsCacheLimitNotExceeded(Dictionary<string, CachedStockMovementData> cache, int maxSize)
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
    }
}
