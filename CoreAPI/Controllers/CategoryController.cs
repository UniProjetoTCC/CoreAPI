using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Data.Context;
using CoreAPI.Models;
using Business.Models;
using Business.Services.Base;
using Business.DataRepositories;
using Business.Enums;
using AutoMapper;
using Business.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<CategoryController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDistributedCache _distributedCache;
        private readonly IMapper _mapper;
        private readonly ILinkedUserService _linkedUserService;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ILinkedUserRepository _linkedUserRepository;

        public CategoryController(
            ILinkedUserService linkedUserService,
            IProductRepository productRepository,
            UserManager<IdentityUser> userManager,
            IUserGroupRepository userGroupRepository,
            ICategoryRepository categoryRepository,
            ILinkedUserRepository linkedUserRepository,
            ILogger<CategoryController> logger,
            IMapper mapper,
            IDistributedCache distributedCache)
        {
            _linkedUserService = linkedUserService;
            _productRepository = productRepository;
            _userManager = userManager;
            _userGroupRepository = userGroupRepository;
            _categoryRepository = categoryRepository;
            _linkedUserRepository = linkedUserRepository;
            _logger = logger;
            _mapper = mapper;
            _distributedCache = distributedCache;
        }

        /// <summary>
        /// Searches for categories based on name with pagination support
        /// </summary>
        /// <remarks>
        /// This endpoint performs an accent-insensitive search on category names and supports caching.
        /// Results are limited to categories belonging to the user's group.
        /// The search results are cached for 1 minute and limited to 5 searches per user.
        /// </remarks>
        /// <param name="name">Category name to search for (required)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 20)</param>
        /// <response code="200">Search results with pagination info</response>
        /// <response code="400">Empty search term</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet("Search")]
        [Authorize]
        public async Task<ActionResult<CategorySearchResponse>> Search([FromQuery] string name, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            const int cacheDurationMinutes = 1;
            const int maxCachedSearchesPerUser = 5;

            if (string.IsNullOrEmpty(name))
                return BadRequest("Category Name is required.");

            // Normalize input for accent-insensitive search
            name = StringUtils.RemoveDiacritics(name);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            string groupId;
            
            if (await _linkedUserService.IsLinkedUserAsync(currentUser.Id))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(currentUser.Id, LinkedUserPermissionsEnum.Product);

                if (!permission)
                {
                    return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
                }

                // Get the group ID from the linked user
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(currentUser.Id);
                groupId = linkedUser?.GroupId ?? string.Empty;
                
            }else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
                groupId = group?.GroupId ?? string.Empty;
            }

            // Generate cache key for this user's search context
            string searchesMapKey = $"category_searches:{currentUser.Id}:{groupId}";
            
            try
            {
                // Try to get the map of prior searches from Redis
                Dictionary<string, CachedCategorySearch>? userSearchCache = null;
                var cachedSearchesBytes = await _distributedCache.GetAsync(searchesMapKey);
                
                if (cachedSearchesBytes != null)
                {
                    // Deserialize the map of searches
                    var cachedSearchesJson = Encoding.UTF8.GetString(cachedSearchesBytes);
                    userSearchCache = JsonSerializer.Deserialize<Dictionary<string, CachedCategorySearch>?>(
                        cachedSearchesJson, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (userSearchCache != null)
                    {
                        // Find a previous search that is a prefix of the current search
                        var matchingPrefixEntry = userSearchCache
                            .Where(kv => name.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase) && kv.Key.Length > 0)
                            .OrderByDescending(kv => kv.Key.Length)  // Get the longest matching prefix
                            .FirstOrDefault();
                        
                        if (!matchingPrefixEntry.Equals(default(KeyValuePair<string, CachedCategorySearch>)))
                        {
                            // We have a cache hit with a previous search term that is a prefix of the current one
                            // Filter the cached results for this new, more specific search term
                            var filteredResults = matchingPrefixEntry.Value.Categories
                                .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase) || 
                                       (c.Description != null && c.Description.Contains(name, StringComparison.OrdinalIgnoreCase)))
                                .ToList();
                                
                            // Add or update the cache with this new search term
                            userSearchCache[name] = new CachedCategorySearch
                            {
                                Categories = filteredResults,
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
                                .Select(c => _mapper.Map<CategoryDto>(c))
                                .ToList();
                                
                            return Ok(new CategorySearchResponse 
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
                userSearchCache ??= new Dictionary<string, CachedCategorySearch>();
                
                var (results, totalCount) = await _categoryRepository.SearchByNameAsync(
                    name: name,
                    groupId: groupId,
                    page: page,
                    pageSize: pageSize
                );
                
                // Add this new search to the cache
                userSearchCache[name] = new CachedCategorySearch
                {
                    Categories = results,
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
                
                // Map results to DTOs using AutoMapper
                var categoryDtos = results
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => _mapper.Map<CategoryDto>(c))
                    .ToList();
                
                return Ok(new CategorySearchResponse 
                {
                    Items = categoryDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Pages = (int)Math.Ceiling((double)totalCount / pageSize),
                    FromCache = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using cache for category search. Falling back to database.");
                // If there's any error with the cache, fall back to the database
                var (results, totalCount) = await _categoryRepository.SearchByNameAsync(
                    name: name,
                    groupId: groupId,
                    page: page,
                    pageSize: pageSize
                );
                
                // Map results to DTOs using AutoMapper
                var categoryDtos = results
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => _mapper.Map<CategoryDto>(c))
                    .ToList();
                
                var response = new CategorySearchResponse 
                {
                    Items = categoryDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Pages = (int)Math.Ceiling((double)totalCount / pageSize),
                    FromCache = false
                };
                
                return Ok(response);
            }
        }

        // Helper method to ensure cache doesn't exceed the limit by removing oldest entries
        private void EnsureCacheLimitNotExceeded(Dictionary<string, CachedCategorySearch> cache, int maxSize)
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
        /// Gets a specific category by ID
        /// </summary>
        /// <remarks>
        /// Retrieves detailed information about a category by its ID.
        /// Only returns categories that belong to the user's group.
        /// </remarks>
        /// <param name="id">Category ID (required)</param>
        /// <response code="200">Category details</response>
        /// <response code="400">Invalid or missing category ID</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Category not found</response>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Category ID is required");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(currentUser.Id))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(currentUser.Id, LinkedUserPermissionsEnum.Product);

                if (!permission)
                {
                    return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
                }

                // Get the group ID from the linked user
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(currentUser.Id);
                groupId = linkedUser?.GroupId ?? string.Empty;
                
            }
            else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
                groupId = group?.GroupId ?? string.Empty;
            }

            var category = await _categoryRepository.GetByIdAsync(id, groupId);
            if (category == null) return NotFound("Category not found");

            var categoryDto = _mapper.Map<CategoryDto>(category);
            return Ok(categoryDto);
        }

        /// <summary>
        /// Creates a new category
        /// </summary>
        /// <remarks>
        /// This endpoint creates a new category within the user's group with validation:
        /// - Checks for category name uniqueness within the group
        /// - Associates the category with the user's current group
        /// 
        /// Categories are used to organize products and can be assigned to multiple products.
        /// </remarks>
        /// <param name="model">Category creation model with required fields</param>
        /// <response code="200">Created category details</response>
        /// <response code="400">Validation errors</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateCategoryAsync([FromBody] CategoryCreateModel model)
        {
            if (model == null) return BadRequest("Category model is required");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(currentUser.Id))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(currentUser.Id, LinkedUserPermissionsEnum.Product);

                if (!permission)
                {
                    return StatusCode(403 ,"You do not have permission to access this resource.");
                }

                // Get the group ID from the linked user service
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(currentUser.Id);
                groupId = linkedUser?.GroupId ?? string.Empty;
            }
            else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
                groupId = group?.GroupId ?? string.Empty;
            }

            // Check if the category already exists
            var existingCategory = await _categoryRepository.GetByNameAsync(
                name: model.Name,
                groupId: groupId);

            if (existingCategory != null)
            {
                return Conflict("A category with the same name already exists.");
            }

            // Create a new category if it doesn't exist
            var newCategory = await _categoryRepository.CreateCategoryAsync(
                name: model.Name,
                groupId: groupId,
                description: model.Description,
                active: model.Active);

            if (newCategory == null) return StatusCode(500, "An error occurred while creating the category.");

            var categoryDto = _mapper.Map<CategoryDto>(newCategory);
            return CreatedAtAction(nameof(Get), new { id = newCategory.Id }, categoryDto);
        }

        /// <summary>
        /// Updates an existing category
        /// </summary>
        /// <remarks>
        /// This endpoint updates a category with the following validations:
        /// - Verifies the category exists and belongs to the user's group
        /// - Updates all specified category properties
        /// 
        /// Only categories within the user's group can be updated.
        /// </remarks>
        /// <param name="model">Category update model with all fields to update</param>
        /// <response code="200">Updated category details</response>
        /// <response code="400">Validation errors or category not found in user's group</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Category not found</response>
        [HttpPut]
        [Authorize]
        public async Task<ActionResult> UpdateCategoryAsync([FromBody] CategoryUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(currentUser.Id))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(currentUser.Id, LinkedUserPermissionsEnum.Product);

                if (!permission)
                {
                    return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
                }

                // Get the group ID from the linked user
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(currentUser.Id);
                groupId = linkedUser?.GroupId ?? string.Empty;

            }
            else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
                groupId = group?.GroupId ?? string.Empty;
            }

            // Verify if category exists and belongs to the group
            if (!string.IsNullOrEmpty(model.Id))
            {
                var existingCategory = await _categoryRepository.GetByIdAsync(model.Id, groupId);
                if (existingCategory == null)
                {
                    return BadRequest("The specified category does not exist or does not belong to your group.");
                }
            }
            else
            {
                return BadRequest("Category ID is required.");
            }

            // Update the product using individual fields
            var category = await _categoryRepository.UpdateCategoryAsync(
                id: model.Id,
                name: model.Name,
                description: model.Description,
                active: model.Active);

            if (category == null)
            {
                return NotFound("Category not found or update failed.");
            }

            _logger.LogInformation($"Categoria atualizada: ID={category.Id}, GroupID={category.GroupId}");
            var categoryDto = _mapper.Map<CategoryDto>(category);
            return Ok(categoryDto);

        }

        /// <summary>
        /// Deletes a category by ID
        /// </summary>
        /// <remarks>
        /// This endpoint permanently deletes a category with the following validations:
        /// - Verifies the category exists and belongs to the user's group
        /// - Confirms the user has permission to delete categories
        /// - Checks that the category has no associated products
        /// 
        /// Categories that have products associated with them cannot be deleted.
        /// </remarks>
        /// <param name="id">Category ID to delete</param>
        /// <response code="204">Category successfully deleted (no content)</response>
        /// <response code="400">Invalid ID, category has products, or other validation errors</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Category not found</response>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteCategoryAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Category ID is required");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(currentUser.Id))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(currentUser.Id, LinkedUserPermissionsEnum.Product);

                if (!permission)
                {
                    return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
                }

                // Get the group ID from the linked user
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(currentUser.Id);
                groupId = linkedUser?.GroupId ?? string.Empty;

            }
            else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
                groupId = group?.GroupId ?? string.Empty;
            }

            // Check if the category exists and belongs to the group
            var existingCategory = await _categoryRepository.GetByIdAsync(id, groupId);
            if (existingCategory == null) return NotFound("Category not found or does not belong to your group.");

            bool hasProducts = await _productRepository.HasProductsInCategoryAsync(id, groupId);
            if(hasProducts)
            {
                return BadRequest("Cannot delete category with existing products.");
            }

            // Delete the category
            var deletedCategory = await _categoryRepository.DeleteCategoryAsync(id, groupId);
            if (deletedCategory == null) return NotFound();

            // Return 204 No Content for successful deletion operations
            return NoContent();
        }

       
    }
}