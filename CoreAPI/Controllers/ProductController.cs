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
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Business.Utils;

namespace CoreAPI.Controllers 
{
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
            _mapper = mapper;
            _distributedCache = distributedCache;
            _logger = logger;
        }

        
        [HttpGet("Search")]
        [Authorize]
        public async Task<ActionResult<ProductSearchResponse>> Search([FromQuery] string name, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            int cacheDurationMinutes = 1;

            if (string.IsNullOrEmpty(name))
                return BadRequest("Product Name is required.");

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
            string searchesMapKey = $"product_searches:{currentUser.Id}:{groupId}";
            
            try
            {
                // Try to get the map of prior searches from Redis
                Dictionary<string, List<ProductBusinessModel>>? userSearchCache = null;
                var cachedSearchesBytes = await _distributedCache.GetAsync(searchesMapKey);
                
                if (cachedSearchesBytes != null)
                {
                    // Deserialize the map of searches
                    var cachedSearchesJson = Encoding.UTF8.GetString(cachedSearchesBytes);
                    userSearchCache = JsonSerializer.Deserialize<Dictionary<string, List<ProductBusinessModel>>?>(
                        cachedSearchesJson, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (userSearchCache != null)
                    {
                        // Find a previous search that is a prefix of the current search
                        var matchingPrefixEntry = userSearchCache
                            .Where(kv => name.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase) && kv.Key.Length > 0)
                            .OrderByDescending(kv => kv.Key.Length)  // Get the longest matching prefix
                            .FirstOrDefault();
                        
                        if (!matchingPrefixEntry.Equals(default(KeyValuePair<string, List<ProductBusinessModel>>)))
                        {
                            // We have a cache hit with a previous search term that is a prefix of the current one
                            // Filter the cached results for this new, more specific search term
                            var filteredResults = matchingPrefixEntry.Value
                                .Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                                .ToList();
                                
                            // Update the cache with this new search term
                            userSearchCache[name] = filteredResults;
                            
                            // Store the updated cache
                            var serializedCache = JsonSerializer.Serialize(userSearchCache);
                            var cacheOptions = new DistributedCacheEntryOptions()
                                .SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));
                                
                            await _distributedCache.SetAsync(
                                searchesMapKey, 
                                Encoding.UTF8.GetBytes(serializedCache), 
                                cacheOptions);
                            
                            // Paginate the results
                            var paginatedResults = filteredResults
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
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
                userSearchCache ??= new Dictionary<string, List<ProductBusinessModel>>();
                
                var (results, totalCount) = await _productRepository.SearchByNameAsync(
                    name: name,
                    groupId: groupId,
                    page: page,
                    pageSize: pageSize
                );
                
                // Add this new search to the cache
                userSearchCache[name] = results;
                
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
                    Items = results,
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
                
                var response = new ProductSearchResponse 
                {
                    Items = results,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Pages = (int)Math.Ceiling((double)totalCount / pageSize),
                    FromCache = false
                };
                
                return Ok(response);
            }
        }

        [HttpGet("Get")]
        [Authorize]
        public async Task<ActionResult> Get([FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Product ID is required.");
                
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

            var product = await _productRepository.GetById(id, groupId);

            return product != null ? Ok(product) : NotFound();
        }

        [HttpPost("Create")]
        [Authorize]
        public async Task<ActionResult> CreateProductAsync([FromBody] ProductCreateModel model)
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
                
            }else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
                groupId = group?.GroupId ?? string.Empty;
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

            return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
        }

        [HttpPost("Update")]
        [Authorize]
        public async Task<ActionResult> UpdateProductAsync([FromBody] ProductUpdateModel model)
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
                
            }else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
                groupId = group?.GroupId ?? string.Empty;
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
            
            // Update the product using individual fields
            var product = await _productRepository.UpdateProductAsync(
                id: model.Id,
                groupId: groupId,
                categoryId: model.CategoryId,
                name: model.Name,
                sku: model.SKU,
                barCode: model.BarCode,
                description: model.Description,
                price: model.Price,
                cost: model.Cost,
                active: model.Active
            );


            return product != null ? Ok(product) : NotFound();
        }

        [HttpDelete("Delete")]
        [Authorize]
        public async Task<ActionResult> DeleteProductAsync([FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Product ID is required");

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

            return Ok(deletedProduct);
        }
    }
}