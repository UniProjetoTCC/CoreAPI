using AutoMapper;
using Business.DataRepositories;
using Business.Enums;
using Business.Services.Base;
using Business.Utils;
using CoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace CoreAPI.Controllers
{
    /// <summary>
    /// Controller responsible for managing products.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ILinkedUserService _linkedUserService;
        private readonly IProductRepository _productRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IProductPromotionRepository _productPromotionRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            ILinkedUserService linkedUserService,
            IProductRepository productRepository,
            UserManager<IdentityUser> userManager,
            IUserGroupRepository userGroupRepository,
            ICategoryRepository categoryRepository,
            ILinkedUserRepository linkedUserRepository,
            IStockRepository stockRepository,
            IProductPromotionRepository productPromotionRepository,
            IMapper mapper,
            IDistributedCache distributedCache,
            ILogger<ProductController> logger)
        {
            _linkedUserService = linkedUserService;
            _productRepository = productRepository;
            _userManager = userManager;
            _userGroupRepository = userGroupRepository;
            _categoryRepository = categoryRepository;
            _linkedUserRepository = linkedUserRepository;
            _stockRepository = stockRepository;
            _productPromotionRepository = productPromotionRepository;
            _mapper = mapper;
            _distributedCache = distributedCache;
            _logger = logger;
        }


        /// <summary>
        /// Searches for products based on name with pagination support
        /// </summary>
        /// <remarks>
        /// This endpoint performs an accent-insensitive search on product names and supports caching.
        /// Results are limited to products belonging to the user's group.
        /// The search results are cached for 1 minute and limited to 5 searches per user.
        /// </remarks>
        /// <param name="name">Product name to search for (optional)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 20)</param>
        /// <response code="200">Search results with pagination info</response>
        /// <response code="400">Empty search term</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet("Search")]
        [Authorize]
        public async Task<ActionResult<ProductSearchResponse>> Search([FromQuery] string name = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            const int cacheDurationMinutes = 1;
            const int maxCachedSearchesPerUser = 5;

            // Allow empty name as wildcard; only validate pagination
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page number and page size must be greater than zero.");

            // Normalize input for accent-insensitive search
            name = StringUtils.RemoveDiacritics(name);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckProductPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            // If name is empty, return all products paginated with caching
            if (string.IsNullOrWhiteSpace(name))
            {
                // Generate cache key for all products
                string allProductsCacheKey = $"all_products:{groupId}:{page}:{pageSize}";

                // Try to get from cache first
                var cachedResultBytes = await _distributedCache.GetAsync(allProductsCacheKey);
                if (cachedResultBytes != null)
                {
                    try
                    {
                        // Deserialize the cached result
                        var cachedResultJson = Encoding.UTF8.GetString(cachedResultBytes);
                        var cachedResponse = JsonSerializer.Deserialize<ProductSearchResponse>(
                            cachedResultJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (cachedResponse != null)
                        {
                            cachedResponse.FromCache = true;
                            return Ok(cachedResponse);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deserializing cached all products result");
                        // Continue to fetch from database on error
                    }
                }

                // If not in cache or error occurred, get from database
                var (allItems, totalCount) = await _productRepository.SearchByNameAsync("", groupId, page, pageSize);
                var dtos = allItems.Select(p => _mapper.Map<ProductDto>(p)).ToList();

                var response = new ProductSearchResponse
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Pages = (int)Math.Ceiling((double)totalCount / pageSize),
                    FromCache = false
                };

                // Cache the result
                try
                {
                    var serializedResponse = JsonSerializer.Serialize(response);
                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));

                    await _distributedCache.SetAsync(
                        allProductsCacheKey,
                        Encoding.UTF8.GetBytes(serializedResponse),
                        cacheOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error caching all products result");
                    // Continue without caching on error
                }

                return Ok(response);
            }

            // Generate cache key for this user's search context
            string searchesMapKey = $"product_searches:{currentUser.Id}:{groupId}";

            try
            {
                // Try to get the map of prior searches from Redis
                Dictionary<string, CachedProductSearch>? userSearchCache = null;
                var cachedSearchesBytes = await _distributedCache.GetAsync(searchesMapKey);

                if (cachedSearchesBytes != null)
                {
                    // Deserialize the map of searches
                    var cachedSearchesJson = Encoding.UTF8.GetString(cachedSearchesBytes);
                    userSearchCache = JsonSerializer.Deserialize<Dictionary<string, CachedProductSearch>?>(
                        cachedSearchesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (userSearchCache != null)
                    {
                        // Find a previous search that is a prefix of the current search
                        var matchingPrefixEntry = userSearchCache
                            .Where(kv => name.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase) && kv.Key.Length > 0)
                            .OrderByDescending(kv => kv.Key.Length)  // Get the longest matching prefix
                            .FirstOrDefault();

                        if (!matchingPrefixEntry.Equals(default(KeyValuePair<string, CachedProductSearch>)))
                        {
                            // We have a cache hit with a previous search term that is a prefix of the current one
                            // Filter the cached results for this new, more specific search term
                            var filteredResults = matchingPrefixEntry.Value.Products
                                .Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            // Add or update the cache with this new search term
                            userSearchCache[name] = new CachedProductSearch
                            {
                                Products = filteredResults,
                                Timestamp = DateTime.UtcNow
                            };

                            // Ensure we don't exceed the cache limit per user
                            EnsureCacheLimitNotExceeded(userSearchCache, maxCachedSearchesPerUser);

                            // Store the updated cache
                            var serializedCache = JsonSerializer.Serialize(userSearchCache);
                            var cacheOptions = new DistributedCacheEntryOptions()
                                .SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));

                            await _distributedCache.SetAsync(
                                searchesMapKey,
                                Encoding.UTF8.GetBytes(serializedCache),
                                cacheOptions);

                            // Paginate the results and map to DTOs
                            var paginatedResults = filteredResults
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .Select(p => _mapper.Map<ProductDto>(p))
                                .ToList();

                            return Ok(new ProductSearchResponse
                            {
                                Items = paginatedResults,
                                TotalCount = filteredResults.Count,
                                Page = page,
                                PageSize = pageSize,
                                Pages = (int)Math.Ceiling((double)filteredResults.Count / pageSize),
                                FromCache = true
                            });
                        }
                    }
                }

                // If we get here, either there was no cache or no matching prefix was found
                // Let's do a database search with full-text capabilities
                userSearchCache ??= new Dictionary<string, CachedProductSearch>();

                var (results, totalCount) = await _productRepository.SearchByNameAsync(
                    name: name,
                    groupId: groupId,
                    page: page,
                    pageSize: pageSize
                );

                // Map results to DTOs using AutoMapper
                var productDtos = results
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => _mapper.Map<ProductDto>(p))
                    .ToList();

                // Add this new search to the cache
                userSearchCache[name] = new CachedProductSearch
                {
                    Products = results,
                    Timestamp = DateTime.UtcNow
                };

                // Ensure we don't exceed the cache limit per user
                EnsureCacheLimitNotExceeded(userSearchCache, maxCachedSearchesPerUser);

                // Update the cache
                var newSerializedCache = JsonSerializer.Serialize(userSearchCache);
                var newCacheOptions = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));

                await _distributedCache.SetAsync(
                    searchesMapKey,
                    Encoding.UTF8.GetBytes(newSerializedCache),
                    newCacheOptions);

                return Ok(new ProductSearchResponse
                {
                    Items = productDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Pages = (int)Math.Ceiling((double)totalCount / pageSize),
                    FromCache = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using cache for product search. Falling back to database.");
                // If there's any error with the cache, fall back to the database
                var (results, totalCount) = await _productRepository.SearchByNameAsync(
                    name: name,
                    groupId: groupId,
                    page: page,
                    pageSize: pageSize
                );

                // Map results to DTOs using AutoMapper
                var productDtos = results
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => _mapper.Map<ProductDto>(p))
                    .ToList();

                var response = new ProductSearchResponse
                {
                    Items = productDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Pages = (int)Math.Ceiling((double)totalCount / pageSize),
                    FromCache = false
                };

                return Ok(response);
            }
        }

        private void EnsureCacheLimitNotExceeded(Dictionary<string, CachedProductSearch> cache, int maxSize)
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

        /// <summary>
        /// Gets a specific product by ID
        /// </summary>
        /// <remarks>
        /// Retrieves detailed information about a product by its ID.
        /// Only returns products that belong to the user's group.
        /// </remarks>
        /// <param name="id">Product ID (required)</param>
        /// <response code="200">Product details</response>
        /// <response code="400">Invalid or missing product ID</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Product not found</response>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Product ID is required.");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();


            var (hasPermission, groupId) = await CheckProductPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var product = await _productRepository.GetById(id, groupId);

            if (product == null) return NotFound();

            var productDto = _mapper.Map<ProductDto>(product);
            return Ok(productDto);
        }

        /// <summary>
        /// Creates a new product
        /// </summary>
        /// <remarks>
        /// This endpoint creates a new product with the following validation:
        /// - Checks for duplicate products by barcode or SKU
        /// - Verifies that the category exists and belongs to the user's group
        /// - Associates the product with the user's group
        /// 
        /// The product will be associated with the user's current group and the specified category.
        /// </remarks>
        /// <param name="model">Product creation model with all required fields</param>
        /// <response code="200">Created product details</response>
        /// <response code="400">Validation errors or duplicate product</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateProductAsync([FromBody] ProductCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckProductPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var (canSetInitialStock, _) = await CheckStockPermissionAsync(currentUser.Id);

            // Check for duplicate barcode or SKU in a single database query
            var (isBarcodeDuplicate, isSKUDuplicate, duplicateProduct) =
                await _productRepository.CheckDuplicateProductAsync(
                    barCode: model.BarCode,
                    sku: model.SKU,
                    groupId: groupId
                );

            // Handle barcode duplicates first
            if (isBarcodeDuplicate)
            {
                return Conflict(new
                {
                    error = "DuplicateBarcode",
                    message = $"A product with barcode '{model.BarCode}' already exists in your inventory.",
                    product = duplicateProduct
                });
            }

            // Then handle SKU duplicates
            if (isSKUDuplicate)
            {
                return Conflict(new
                {
                    error = "DuplicateSKU",
                    message = $"A product with SKU '{model.SKU}' already exists in your inventory.",
                    product = duplicateProduct
                });
            }

            // Find category by name or create a new one
            var category = await _categoryRepository.GetByNameAsync(model.CategoryName, groupId);
            string? categoryId;

            if (category == null)
            {
                // Create a new category and get the ID directly
                categoryId = await _categoryRepository.CreateCategoryGetIdAsync(
                    name: model.CategoryName,
                    groupId: groupId,
                    description: $"Category automatically created for product {model.Name}",
                    active: true
                );

                if (categoryId == null)
                {
                    return BadRequest("Failed to create category");
                }
            }
            else
            {
                categoryId = category.Id;
            }

            try
            {
                // Create the product using individual fields
                var product = await _productRepository.CreateProductAsync(
                    groupId: groupId,
                    categoryId: categoryId,
                    name: model.Name,
                    sku: model.SKU,
                    barCode: model.BarCode,
                    description: model.Description,
                    price: model.Price,
                    cost: model.Cost,
                    active: model.Active
                );

                if (product == null) return StatusCode(500, "An error occurred while creating the product.");

                // Add initial stock if specified and greater than zero
                if (model.InitialStock > 0 && canSetInitialStock)
                {
                    await _stockRepository.AddStockAsync(
                        productId: product.Id,
                        groupId: groupId,
                        quantity: (int)model.InitialStock,
                        userId: currentUser.Id
                    );
                }

                var productDto = _mapper.Map<ProductDto>(product);
                return CreatedAtAction(nameof(Get), new { id = product.Id }, productDto);
            }
            catch (DbUpdateException ex)
            {
                // Handle any other database-related errors not caught by earlier checks
                if (ex.InnerException?.Message.Contains("IX_Products_GroupId_BarCode") == true)
                {
                    return Conflict(new
                    {
                        error = "DuplicateBarcode",
                        message = $"A product with barcode '{model.BarCode}' already exists in your inventory."
                    });
                }
                else if (ex.InnerException?.Message.Contains("IX_Products_GroupId_SKU") == true)
                {
                    return Conflict(new
                    {
                        error = "DuplicateSKU",
                        message = $"A product with SKU '{model.SKU}' already exists in your inventory."
                    });
                }

                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, "An error occurred while creating the product. Please try again.");
            }
        }

        /// <summary>
        /// Updates an existing product
        /// </summary>
        /// <remarks>
        /// This endpoint updates a product with the following validations:
        /// - Verifies the product exists and belongs to the user's group
        /// - Checks that the category exists and belongs to the user's group
        /// - Updates all specified product properties EXCEPT price and active status
        /// - Price changes are only allowed through the dedicated price update endpoint
        /// - Active status changes are only allowed through the dedicated Activate/Deactivate endpoints
        /// 
        /// Only products within the user's group can be updated.
        /// </remarks>
        /// <param name="id">Product ID to update</param>
        /// <param name="model">Product update model with all fields to update</param>
        /// <response code="200">Updated product details</response>
        /// <response code="400">Validation errors or product not found in user's group</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions or attempting to change price</response>
        /// <response code="404">Product not found</response>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateProductAsync(string id, ProductUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckProductPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            // Verify if category exists and belongs to the group
            if (!string.IsNullOrEmpty(model.CategoryId))
            {
                var category = await _categoryRepository.GetByIdAsync(model.CategoryId, groupId);
                if (category == null)
                {
                    return BadRequest("The specified category does not exist or does not belong to your group.");
                }
            }
            else
            {
                return BadRequest("Category ID is required.");
            }

            // Update the product using individual fields, preserving the active status
            var product = await _productRepository.UpdateProductAsync(
                id: id,
                groupId: groupId,
                categoryId: model.CategoryId,
                name: model.Name,
                sku: model.SKU,
                barCode: model.BarCode,
                description: model.Description,
                cost: model.Cost
            );

            if (product == null) return NotFound();

            var productDto = _mapper.Map<ProductDto>(product);
            return Ok(productDto);
        }

        /// <summary>
        /// Deletes a product by ID
        /// </summary>
        /// <remarks>
        /// This endpoint permanently deletes a product with the following validations:
        /// - Verifies the product exists and belongs to the user's group
        /// - Confirms the user has permission to delete products
        /// 
        /// Once deleted, a product cannot be recovered.
        /// </remarks>
        /// <param name="id">Product ID to delete</param>
        /// <response code="204">Product successfully deleted (no content)</response>
        /// <response code="400">Invalid ID or product not found in user's group</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Product not found</response>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteProductAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Product ID is required");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckProductPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            if (!string.IsNullOrEmpty(id))
            {
                var existingProduct = await _productRepository.GetById(id, groupId);
                if (existingProduct == null)
                {
                    return BadRequest("The specified product does not exist or does not belong to your group.");
                }
            }
            else
            {
                return BadRequest("Product ID is required.");
            }

            var deletedProduct = await _productRepository.DeleteProductAsync(id, groupId);

            if (deletedProduct == null)
            {
                return NotFound("Product not found or deletion failed.");
            }

            // Return 204 No Content for successful deletion operations
            return NoContent();
        }


        /// <summary>
        /// Updates a product's price and records the change in price history
        /// </summary>
        /// <remarks>
        /// This endpoint is the only way to update a product's price. It performs the following:
        /// - Verifies the product exists and belongs to the user's group
        /// - Updates the product's price
        /// - Records the price change in the PriceHistory table with the old and new prices
        /// - Optionally records a reason for the price change
        /// </remarks>
        /// <param name="id">Product ID to update price for</param>
        /// <param name="model">Price update model with product ID, new price, and optional reason</param>
        /// <response code="200">Updated product details</response>
        /// <response code="400">Validation errors or product not found in user's group</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Product not found</response>
        [HttpPut("UpdatePrice/{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateProductPriceAsync(string id, ProductPriceUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckProductPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            // Get the existing product
            var existingProduct = await _productRepository.GetById(id, groupId);
            if (existingProduct == null)
            {
                return NotFound("Product not found or does not belong to your group.");
            }

            // Store the old price for history
            decimal oldPrice = existingProduct.Price;

            // Update the product price
            var updatedProduct = await _productRepository.UpdateProductPriceAsync(
                id: id,
                groupId: groupId,
                newPrice: model.NewPrice,
                userId: currentUser.Id,
                reason: model.Reason
            );

            if (updatedProduct == null) return NotFound();

            var productDto = _mapper.Map<ProductDto>(updatedProduct);
            return Ok(productDto);
        }

        

        /// <summary>
        /// Activates a product
        /// </summary>
        /// <remarks>
        /// This endpoint activates a product that is currently inactive.
        /// If the product is already active, it returns a BadRequest with an error message.
        /// </remarks>
        /// <param name="id">Product ID to activate</param>
        /// <response code="200">Product successfully activated</response>
        /// <response code="400">Product is already active or invalid ID</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Product not found</response>
        [HttpPatch("{id}/Activate")]
        [Authorize]
        public async Task<ActionResult> ActivateProductAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Product ID is required.");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckProductPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            // Check if product exists and get its current state
            var existingProduct = await _productRepository.GetById(id, groupId);
            if (existingProduct == null)
            {
                return NotFound("Product not found or does not belong to your group.");
            }

            // Check if product is already active
            if (existingProduct.Active)
            {
                return BadRequest("This product is already active.");
            }

            // Activate the product
            var activatedProduct = await _productRepository.ActivateAsync(id, groupId);
            if (activatedProduct == null)
            {
                return StatusCode(500, "An error occurred while activating the product.");
            }

            var productDto = _mapper.Map<ProductDto>(activatedProduct);
            return Ok(productDto);
        }

        /// <summary>
        /// Deactivates a product
        /// </summary>
        /// <remarks>
        /// This endpoint deactivates a product that is currently active.
        /// If the product is already inactive, it returns a BadRequest with an error message.
        /// </remarks>
        /// <param name="id">Product ID to deactivate</param>
        /// <response code="200">Product successfully deactivated</response>
        /// <response code="400">Product is already inactive or invalid ID</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Product not found</response>
        [HttpPatch("{id}/Deactivate")]
        [Authorize]
        public async Task<ActionResult> DeactivateProductAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Product ID is required.");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckProductPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            // Check if product exists and get its current state
            var existingProduct = await _productRepository.GetById(id, groupId);
            if (existingProduct == null)
            {
                return NotFound("Product not found or does not belong to your group.");
            }

            // Check if product is already inactive
            if (!existingProduct.Active)
            {
                return BadRequest("This product is already inactive.");
            }

            // Deactivate the product
            var deactivatedProduct = await _productRepository.DeactivateAsync(id, groupId);
            if (deactivatedProduct == null)
            {
                return StatusCode(500, "An error occurred while deactivating the product.");
            }

            var productDto = _mapper.Map<ProductDto>(deactivatedProduct);
            return Ok(productDto);
        }

        /// <summary>
        /// Gets all promotions a specific product is part of.
        /// </summary>
        /// <remarks>
        /// Retrieves a list of all promotions linked to the specified product ID.
        /// Only returns promotions from the user's group.
        /// </remarks>
        /// <param name="id">Product ID (required)</param>
        /// <response code="200">List of promotions for the product</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Product not found</response>
        [HttpGet("{id}/Promotions")]
        [Authorize]
        [ProducesResponseType(typeof(List<PromotionDto>), 200)]
        public async Task<ActionResult<IEnumerable<PromotionDto>>> GetPromotionsForProductAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Product ID is required.");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionAndProductPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource.");
            }

            // First, check if the product exists and belongs to the user's group
            var product = await _productRepository.GetById(id, groupId);
            if (product == null)
            {
                return NotFound("Product not found or does not belong to your group.");
            }
            
            // Se precisar injetar o IProductPromotionRepository, adicione-o no construtor.
            // Assumindo que ele já está injetado.
            var promotions = await _productPromotionRepository.GetPromotionsByProductIdAsync(id, groupId);

            if (promotions == null || !promotions.Any())
            {
                return Ok(new List<PromotionDto>()); // Return an empty list if no promotions are found
            }

            var promotionDtos = _mapper.Map<List<PromotionDto>>(promotions);
            return Ok(promotionDtos);
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

        private async Task<(bool hasPermission, string groupId)> CheckProductPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Product);

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

        private async Task<(bool hasPermission, string groupId)> CheckPromotionAndProductPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                bool hasProductPermission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Product);
                bool hasPromotionPermission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Promotion);
                
                if (!hasProductPermission)
                {
                    return (false, string.Empty);
                }

                if (!hasPromotionPermission)
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