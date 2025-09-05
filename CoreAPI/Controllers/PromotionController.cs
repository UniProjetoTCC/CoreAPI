using AutoMapper;
using Business.DataRepositories;
using Business.Enums;
using Business.Models;
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
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IProductPromotionRepository _productPromotionRepository;
        private readonly IProductRepository _productRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<PromotionController> _logger;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _distributedCache;
        private readonly ILinkedUserService _linkedUserService;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly int cacheDurationMinutes = 1;

        public PromotionController(
            IPromotionRepository promotionRepository,
            IProductPromotionRepository productPromotionRepository,
            IProductRepository productRepository,
            UserManager<IdentityUser> userManager,
            ILogger<PromotionController> logger,
            IMapper mapper,
            IDistributedCache distributedCache,
            ILinkedUserService linkedUserService,
            ILinkedUserRepository linkedUserRepository,
            IUserGroupRepository userGroupRepository)
        {
            _promotionRepository = promotionRepository;
            _productPromotionRepository = productPromotionRepository;
            _productRepository = productRepository;
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
            _distributedCache = distributedCache;
            _linkedUserService = linkedUserService;
            _linkedUserRepository = linkedUserRepository;
            _userGroupRepository = userGroupRepository;
        }

        /// <summary>
        /// Search promotions by name with pagination and caching
        /// </summary>
        /// <remarks>
        /// This endpoint allows searching promotions by name with the following features:
        /// - Search with or without term (name)
        /// - Paginated results
        /// - Cache for frequent searches
        /// 
        /// All users with group access can view promotions.
        /// </remarks>
        /// <param name="name">Search term (name) (optional)</param>
        /// <param name="page">Page number (starts at 1)</param>
        /// <param name="pageSize">Page size</param>
        /// <response code="200">List of promotions found</response>
        /// <response code="400">Invalid pagination parameters</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to access the resource</response>
        [HttpGet("Search")]
        [ProducesResponseType(typeof(PromotionSearchResponse), 200)]
        public async Task<IActionResult> SearchPromotionsAsync(
            [FromQuery] string name = "",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest("Page number and page size must be greater than zero.");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            try
            {
                // If it's a search without term
                if (string.IsNullOrWhiteSpace(name))
                {
                    // Generate cache key for all promotions
                    string allPromotionsCacheKey = $"all_promotions:{groupId}:{page}:{pageSize}";

                    // Try to get from cache first
                    var cachedResultBytes = await _distributedCache.GetAsync(allPromotionsCacheKey);
                    if (cachedResultBytes != null)
                    {
                        try
                        {
                            // Deserialize cached result
                            var cachedResultJson = Encoding.UTF8.GetString(cachedResultBytes);
                            var cachedResponse = JsonSerializer.Deserialize<PromotionSearchResponse>(
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
                            _logger.LogError(ex, "Error deserializing cached all promotions result");
                            // Keep going without cache in case of error
                        }
                    }

                    // If not in cache or error, search in the database
                    var (dbItems, dbTotalCount) = await _promotionRepository.SearchByNameAsync("", groupId, page, pageSize);
                    var dbDtos = dbItems.Select(p => _mapper.Map<PromotionDto>(p)).ToList();

                    var dbResponse = new PromotionSearchResponse
                    {
                        Items = dbDtos,
                        TotalCount = dbTotalCount,
                        Page = page,
                        PageSize = pageSize,
                        Pages = (int)Math.Ceiling((double)dbTotalCount / pageSize),
                        FromCache = false
                    };

                    // Store in cache
                    try
                    {
                        var serializedResponse = JsonSerializer.Serialize(dbResponse);
                        var cacheOptions = new DistributedCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));

                        await _distributedCache.SetAsync(
                            allPromotionsCacheKey,
                            Encoding.UTF8.GetBytes(serializedResponse),
                            cacheOptions);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error caching all promotions result");
                        // Keep going without cache in case of error
                    }
                    
                    return Ok(dbResponse);
                }

                // Search by name
                // Generate cache key for the search context of this user
                string searchesMapKey = $"promotion_searches:{currentUser.Id}:{groupId}";

                // Try to get the previous search map from Redis
                Dictionary<string, CachedPromotionSearch>? userSearchCache = null;
                var cachedSearchesBytes = await _distributedCache.GetAsync(searchesMapKey);

                if (cachedSearchesBytes != null)
                {
                    // Deserialize the previous search map from Redis
                    var cachedSearchesJson = Encoding.UTF8.GetString(cachedSearchesBytes);
                    userSearchCache = JsonSerializer.Deserialize<Dictionary<string, CachedPromotionSearch>?>(
                        cachedSearchesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (userSearchCache != null)
                    {
                        // Find a previous search that is a prefix of the current search
                        foreach (var kvp in userSearchCache.OrderByDescending(k => k.Key.Length))
                        {
                            if (name.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase) && 
                                kvp.Value.Page == page && 
                                kvp.Value.PageSize == pageSize)
                            {
                                // Found a cached result that is a prefix of this search
                                var cachedResponse = new PromotionSearchResponse
                                {
                                    Items = kvp.Value.Promotions,
                                    TotalCount = kvp.Value.TotalCount,
                                    Page = page,
                                    PageSize = pageSize,
                                    Pages = (int)Math.Ceiling((double)kvp.Value.TotalCount / pageSize),
                                    FromCache = true
                                };

                                return Ok(cachedResponse);
                            }
                        }
                    }
                }

                // If we get here, we didn't find a cached result that is a prefix of this search, so query the database
                var (searchItems, searchTotalCount) = await _promotionRepository.SearchByNameAsync(name, groupId, page, pageSize);
                var searchDtos = searchItems.Select(p => _mapper.Map<PromotionDto>(p)).ToList();

                var searchResponse = new PromotionSearchResponse
                {
                    Items = searchDtos,
                    TotalCount = searchTotalCount,
                    Page = page,
                    PageSize = pageSize,
                    Pages = (int)Math.Ceiling((double)searchTotalCount / pageSize),
                    FromCache = false
                };

                // Store this search result in cache
                try
                {
                    userSearchCache ??= new Dictionary<string, CachedPromotionSearch>();

                    // Add or update this search in the cache
                    userSearchCache[name] = new CachedPromotionSearch
                    {
                        Promotions = searchDtos,
                        TotalCount = searchTotalCount,
                        Page = page,
                        PageSize = pageSize,
                        Timestamp = DateTime.UtcNow
                    };

                    // Serialize and store the updated cache
                    var serializedCache = JsonSerializer.Serialize(userSearchCache);
                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));

                    await _distributedCache.SetAsync(
                        searchesMapKey,
                        Encoding.UTF8.GetBytes(serializedCache),
                        cacheOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating promotion search cache");
                    // Keep going without cache in case of error
                }

                return Ok(searchResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching promotions");
                return StatusCode(500, "An error occurred while searching promotions. Please try again.");
            }
        }

        /// <summary>
        /// Get a promotion by ID
        /// </summary>
        /// <remarks>
        /// This endpoint returns the details of a specific promotion.
        /// All users with group access can view promotions.
        /// </remarks>
        /// <param name="id">Promotion ID</param>
        /// <response code="200">Promotion details</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to access the resource</response>
        /// <response code="404">Promotion not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PromotionDto), 200)]
        public async Task<IActionResult> GetPromotionById(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            try
            {
                var promotion = await _promotionRepository.GetByIdAsync(id, groupId);
                if (promotion == null) return NotFound();

                var promotionDto = _mapper.Map<PromotionDto>(promotion);
                return Ok(promotionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting promotion by ID");
                return StatusCode(500, "An error occurred while retrieving the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Create a new promotion
        /// </summary>
        /// <remarks>
        /// This endpoint creates a new promotion with the following validations:
        /// - Checks if the user has Promotion permission
        /// - Validates required fields and formats
        /// 
        /// Only users with Promotion permission can create promotions.
        /// </remarks>
        /// <param name="model">Promotion data to be created</param>
        /// <response code="201">Promotion created successfully</response>
        /// <response code="400">Invalid data</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to create promotions</response>
        [HttpPost]
        [ProducesResponseType(typeof(PromotionDto), 201)]
        public async Task<IActionResult> CreatePromotionAsync([FromBody] CreatePromotionRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to create promotions. You need Promotion permission.");
            }

            try
            {
                // Validate dates
                if (model.StartDate >= model.EndDate)
                {
                    return BadRequest("End date must be after start date.");
                }

                var promotion = await _promotionRepository.CreatePromotionAsync(
                    name: model.Name,
                    description: model.Description,
                    discountPercentage: model.DiscountPercentage,
                    startDate: model.StartDate,
                    endDate: model.EndDate,
                    groupId: groupId,
                    isActive: model.IsActive
                );

                if (promotion == null)
                {
                    _logger.LogWarning("Promotion creation returned null for group {GroupId}", groupId);
                    return StatusCode(500, "An error occurred while creating the promotion. Please try again.");
                }

                var promotionDto = _mapper.Map<PromotionDto>(promotion);
                return CreatedAtAction(nameof(GetPromotionById), new { id = promotion.Id }, promotionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promotion");
                return StatusCode(500, "An error occurred while creating the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Update an existing promotion
        /// </summary>
        /// <remarks>
        /// This endpoint updates an existing promotion with the following validations:
        /// - Checks if the promotion exists and belongs to the user's group
        /// - Checks if the user has Promotion permission
        /// - Validates required fields and formats
        /// 
        /// Only users with Promotion permission can update promotions.
        /// </remarks>
        /// <param name="id">Promotion ID to update</param>
        /// <param name="model">Promotion data to be updated</param>
        /// <response code="200">Promotion updated successfully</response>
        /// <response code="400">Invalid data</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to update promotions</response>
        /// <response code="404">Promotion not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(PromotionDto), 200)]
        public async Task<IActionResult> UpdatePromotionAsync(string id, UpdatePromotionRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to update promotions. You need Promotion permission.");
            }

            try
            {
                // Verify if the promotion exists and belongs to the user's group
                var existingPromotion = await _promotionRepository.GetByIdAsync(id, groupId);
                if (existingPromotion == null) return NotFound();

                // Validate dates
                if (model.StartDate >= model.EndDate)
                {
                    return BadRequest("End date must be after start date.");
                }

                var promotion = await _promotionRepository.UpdatePromotionAsync(
                    id: id,
                    groupId: groupId,
                    name: model.Name,
                    description: model.Description,
                    discountPercentage: model.DiscountPercentage,
                    startDate: model.StartDate,
                    endDate: model.EndDate
                );

                if (promotion == null) return NotFound();

                var promotionDto = _mapper.Map<PromotionDto>(promotion);
                return Ok(promotionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating promotion");
                return StatusCode(500, "An error occurred while updating the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Delete a promotion by ID
        /// </summary>
        /// <remarks>
        /// This endpoint deletes a promotion with the following validations:
        /// - Checks if the promotion exists and belongs to the user's group
        /// - Checks if the user has Promotion permission
        /// 
        /// Only users with Promotion permission can delete promotions.
        /// </remarks>
        /// <param name="id">ID of the promotion to be deleted</param>
        /// <response code="200">Promotion deleted successfully</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to delete promotions</response>
        /// <response code="404">Promotion not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(PromotionDto), 200)]
        public async Task<IActionResult> DeletePromotionAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to delete promotions. You need Promotion permission.");
            }

            try
            {
                // Verify if the promotion exists and belongs to the user's group
                var existingPromotion = await _promotionRepository.GetByIdAsync(id, groupId);
                if (existingPromotion == null) return NotFound();

                var promotion = await _promotionRepository.DeletePromotionAsync(id, groupId);
                if (promotion == null) return NotFound();

                var promotionDto = _mapper.Map<PromotionDto>(promotion);
                return Ok(promotionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting promotion");
                return StatusCode(500, "An error occurred while deleting the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Activate a promotion
        /// </summary>
        /// <remarks>
        /// This endpoint activates an inactive promotion with the following validations:
        /// - Checks if the promotion exists and belongs to the user's group
        /// - Checks if the promotion is already active
        /// - Checks if the user has Promotion permission
        /// 
        /// Only users with Promotion permission can activate promotions.
        /// </remarks>
        /// <param name="id">ID of the promotion to be activated</param>
        /// <response code="200">Promotion activated successfully</response>
        /// <response code="400">Promotion is already active</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to activate promotions</response>
        /// <response code="404">Promotion not found</response>
        [HttpPatch("{id}/Activate")]
        [ProducesResponseType(typeof(PromotionDto), 200)]
        public async Task<IActionResult> ActivatePromotionAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to activate promotions. You need Promotion permission.");
            }

            try
            {
                // Verify if the promotion exists and belongs to the user's group
                var existingPromotion = await _promotionRepository.GetByIdAsync(id, groupId);
                if (existingPromotion == null) return NotFound();

                // Verify if the promotion is already active
                if (existingPromotion.IsActive)
                {
                    return BadRequest("This promotion is already active.");
                }

                var promotion = await _promotionRepository.ActivateAsync(id, groupId);
                if (promotion == null) return NotFound();

                var promotionDto = _mapper.Map<PromotionDto>(promotion);
                return Ok(promotionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating promotion");
                return StatusCode(500, "An error occurred while activating the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Deactivate a promotion
        /// </summary>
        /// <remarks>
        /// This endpoint deactivates an active promotion with the following validations:
        /// - Checks if the promotion exists and belongs to the user's group
        /// - Checks if the promotion is already inactive
        /// - Checks if the user has Promotion permission
        /// 
        /// Only users with Promotion permission can deactivate promotions.
        /// </remarks>
        /// <param name="id">ID of the promotion to be deactivated</param>
        /// <response code="200">Promotion deactivated successfully</response>
        /// <response code="400">Promotion is already inactive</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to deactivate promotions</response>
        /// <response code="404">Promotion not found</response>
        [HttpPatch("{id}/Deactivate")]
        [ProducesResponseType(typeof(PromotionDto), 200)]
        public async Task<IActionResult> DeactivatePromotionAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to deactivate promotions. You need Promotion permission.");
            }

            try
            {
                // Verify if the promotion exists and belongs to the user's group
                var existingPromotion = await _promotionRepository.GetByIdAsync(id, groupId);
                if (existingPromotion == null) return NotFound();

                // Verify if the promotion is already inactive
                if (!existingPromotion.IsActive)
                {
                    return BadRequest("This promotion is already inactive.");
                }

                var promotion = await _promotionRepository.DeactivateAsync(id, groupId);
                if (promotion == null) return NotFound();

                var promotionDto = _mapper.Map<PromotionDto>(promotion);
                return Ok(promotionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating promotion");
                return StatusCode(500, "An error occurred while deactivating the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Deactivates all expired promotions
        /// </summary>
        /// <remarks>
        /// This endpoint finds and deactivates all active promotions for the user's group
        /// where the end date has already passed. Requires Promotion permission.
        /// </remarks>
        /// <response code="200">Expired promotions were successfully deactivated. Returns a count of affected promotions.</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to perform this action</response>
        /// <response code="500">An internal error occurred</response>
        [HttpPatch("DeactivateExpired")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> DeactivateExpiredPromotions()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to deactivate promotions. You need Promotion permission.");
            }

            try
            {
                var deactivatedCount = await _promotionRepository.DeactivateExpiredPromotionsAsync(groupId);
                return Ok(new { Message = $"{deactivatedCount} expired promotions have been successfully deactivated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deactivating expired promotions for group {GroupId}", groupId);
                return StatusCode(500, "An internal error occurred while deactivating expired promotions.");
            }
        }

        /// <summary>
        /// Get products in a promotion
        /// </summary>
        /// <remarks>
        /// This endpoint returns all products associated with a specific promotion.
        /// All users with group access can view products in promotions.
        /// </remarks>
        /// <param name="id">Promotion ID</param>
        /// <response code="200">List of products in the promotion</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to access the resource</response>
        /// <response code="404">Promotion not found</response>
        [HttpGet("{id}/Products")]
        [ProducesResponseType(typeof(List<ProductInPromotionDto>), 200)]
        public async Task<IActionResult> GetProductsInPromotionAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            try
            {
                // Verify if the promotion exists and belongs to the user's group
                var existingPromotion = await _promotionRepository.GetByIdAsync(id, groupId);
                if (existingPromotion == null) return NotFound();

                var products = await _productPromotionRepository.GetProductsInPromotionAsync(id, groupId);
                var productBusinessModels = products.Select(p => _mapper.Map<ProductBusinessModel>(p)).ToList();
                var productDtos = productBusinessModels.Select(p => 
                {
                    var dto = _mapper.Map<ProductInPromotionDto>(p);
                    // Calculate promotional price based on discount percentage
                    dto.PromotionalPrice = Math.Round(p.Price * (1 - existingPromotion.DiscountPercentage / 100), 2);
                    return dto;
                }).ToList();

                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products in promotion");
                return StatusCode(500, "An error occurred while retrieving products in the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Add a product to a promotion
        /// </summary>
        /// <remarks>
        /// This endpoint adds a product to a promotion with the following validations:
        /// - Checks if the promotion exists and belongs to the user's group
        /// - Checks if the product exists and belongs to the user's group
        /// - Checks if the user has Promotion permission
        /// 
        /// Only users with Promotion permission can add products to promotions.
        /// </remarks>
        /// <param name="model">Data to add product to promotion</param>
        /// <response code="200">Product added to promotion successfully</response>
        /// <response code="400">Product is already in the promotion</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to add products to promotions</response>
        /// <response code="404">Promotion or product not found</response>
        [HttpPost("AddProduct")]
        [ProducesResponseType(typeof(ProductInPromotionDto), 200)]
        public async Task<IActionResult> AddProductToPromotionAsync([FromBody] AddProductToPromotionRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to add products to promotions. You need Promotion permission.");
            }

            try
            {
                // Verify if the promotion exists and belongs to the user's group
                var existingPromotion = await _promotionRepository.GetByIdAsync(model.PromotionId, groupId);
                if (existingPromotion == null) return NotFound("Promotion not found.");

                if (!existingPromotion.IsActive)
                {
                    return BadRequest("This promotion is inactive.");
                }

                // Verify if the product exists and belongs to the user's group
                var existingProduct = await _productRepository.GetById(model.ProductId, groupId);
                if (existingProduct == null) return NotFound("Product not found.");

                var productPromotion = await _productPromotionRepository.AddProductToPromotionAsync(model.ProductId, model.PromotionId, groupId);
                if (productPromotion == null)
                {
                    return BadRequest("This product is already in the promotion.");
                }

                var productDto = _mapper.Map<ProductInPromotionDto>(productPromotion.Product);
                productDto.PromotionalPrice = Math.Round(productPromotion.Product.Price * (1 - existingPromotion.DiscountPercentage / 100), 2);

                return Ok(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to promotion");
                return StatusCode(500, "An error occurred while adding the product to the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Add multiple products to a promotion
        /// </summary>
        /// <remarks>
        /// This endpoint adds multiple products to a promotion with the following validations:
        /// - Checks if the promotion exists and belongs to the user's group
        /// - Checks if the user has Promotion permission
        /// 
        /// Only users with Promotion permission can add products to promotions.
        /// </remarks>
        /// <param name="model">Data to add products to promotion</param>
        /// <response code="200">Products added to promotion successfully</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to add products to promotions</response>
        /// <response code="404">Promotion not found</response>
        [HttpPost("AddProducts")]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<IActionResult> AddProductsToPromotionAsync([FromBody] AddProductsToPromotionRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to add products to promotions. You need Promotion permission.");
            }

            try
            {
                // Verify if the promotion exists and belongs to the user's group
                var existingPromotion = await _promotionRepository.GetByIdAsync(model.PromotionId, groupId);
                if (existingPromotion == null) return NotFound("Promotion not found.");

                if (!existingPromotion.IsActive)
                {
                    return BadRequest("This promotion is inactive.");
                }

                var addedCount = await _productPromotionRepository.AddProductsToPromotionAsync(model.ProductIds, model.PromotionId, groupId);
                return Ok(addedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding products to promotion");
                return StatusCode(500, "An error occurred while adding products to the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Remove a product from a promotion
        /// </summary>
        /// <remarks>
        /// This endpoint removes a product from a promotion with the following validations:
        /// - Checks if the promotion exists and belongs to the user's group
        /// - Checks if the user has Promotion permission
        /// 
        /// Only users with Promotion permission can remove products from promotions.
        /// </remarks>
        /// <param name="model">Data to remove product from promotion</param>
        /// <response code="200">Product removed from promotion successfully</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to remove products from promotions</response>
        /// <response code="404">Promotion or product not found in promotion</response>
        [HttpPost("RemoveProduct")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> RemoveProductFromPromotionAsync([FromBody] RemoveProductFromPromotionRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to remove products from promotions. You need Promotion permission.");
            }

            try
            {
                // Verify if the promotion exists and belongs to the user's group
                var existingPromotion = await _promotionRepository.GetByIdAsync(model.PromotionId, groupId);
                if (existingPromotion == null) return NotFound("Promotion not found.");

                var result = await _productPromotionRepository.RemoveProductFromPromotionAsync(model.ProductId, model.PromotionId, groupId);
                if (!result)
                {
                    return NotFound("Product not found in the promotion.");
                }

                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product from promotion");
                return StatusCode(500, "An error occurred while removing the product from the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Remove multiple products from a promotion
        /// </summary>
        /// <remarks>
        /// This endpoint removes multiple products from a promotion with the following validations:
        /// - Checks if the promotion exists and belongs to the user's group
        /// - Checks if the user has Promotion permission
        /// 
        /// Only users with Promotion permission can remove products from promotions.
        /// </remarks>
        /// <param name="model">Data to remove products from promotion</param>
        /// <response code="200">Products removed from promotion successfully</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to remove products from promotions</response>
        /// <response code="404">Promotion not found</response>
        [HttpPost("RemoveProducts")]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<IActionResult> RemoveProductsFromPromotionAsync([FromBody] RemoveProductsFromPromotionRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to remove products from promotions. You need Promotion permission.");
            }

            try
            {
                // Verify if the promotion exists and belongs to the user's group
                var existingPromotion = await _promotionRepository.GetByIdAsync(model.PromotionId, groupId);
                if (existingPromotion == null) return NotFound("Promotion not found.");

                var removedCount = await _productPromotionRepository.RemoveProductsFromPromotionAsync(model.ProductIds, model.PromotionId, groupId);
                return Ok(removedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing products from promotion");
                return StatusCode(500, "An error occurred while removing products from the promotion. Please try again.");
            }
        }

        /// <summary>
        /// Remove all products from a promotion
        /// </summary>
        /// <remarks>
        /// This endpoint removes all products from a promotion with the following validations:
        /// - Checks if the promotion exists and belongs to the user's group
        /// - Checks if the user has Promotion permission
        /// 
        /// Only users with Promotion permission can remove products from promotions.
        /// </remarks>
        /// <param name="id">Promotion ID</param>
        /// <response code="200">All products removed from promotion successfully</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to remove products from promotions</response>
        /// <response code="404">Promotion not found</response>
        [HttpPost("{id}/RemoveAllProducts")]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<IActionResult> RemoveAllProductsFromPromotionAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPromotionPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to remove products from promotions. You need Promotion permission.");
            }

            try
            {
                // Verify if the promotion exists and belongs to the user's group
                var existingPromotion = await _promotionRepository.GetByIdAsync(id, groupId);
                if (existingPromotion == null) return NotFound();

                var removedCount = await _productPromotionRepository.RemoveAllProductsFromPromotionAsync(id, groupId);
                return Ok(removedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing all products from promotion");
                return StatusCode(500, "An error occurred while removing all products from the promotion. Please try again.");
            }
        }

        private async Task<(bool hasPermission, string groupId)> CheckPromotionPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Promotion);

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
