using AutoMapper;
using Business.DataRepositories;
using Business.Enums;
using Business.Services.Base;
using Business.Models;
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
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILoyaltyProgramRepository _loyaltyProgramRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CustomerController> _logger;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _distributedCache;
        private readonly ILinkedUserService _linkedUserService;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly int cacheDurationMinutes = 15;

        public CustomerController(
            ICustomerRepository customerRepository,
            ILoyaltyProgramRepository loyaltyProgramRepository,
            UserManager<IdentityUser> userManager,
            ILogger<CustomerController> logger,
            IMapper mapper,
            IDistributedCache distributedCache,
            ILinkedUserService linkedUserService,
            ILinkedUserRepository linkedUserRepository,
            IUserGroupRepository userGroupRepository)
        {
            _customerRepository = customerRepository;
            _loyaltyProgramRepository = loyaltyProgramRepository;
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
            _distributedCache = distributedCache;
            _linkedUserService = linkedUserService;
            _linkedUserRepository = linkedUserRepository;
            _userGroupRepository = userGroupRepository;
        }


        /// <summary>
        /// Search customers by name or document
        /// </summary>
        /// <remarks>
        /// This endpoint allows searching customers by name or document with the following features:
        /// - Search with or without term (name or document)
        /// - Paginated results
        /// - Cache for frequent searches
        /// - Exact search by document
        /// 
        /// All users with group access can view customers.
        /// </remarks>
        /// <param name="term">Search term (name or document) (optional)</param>
        /// <param name="document">Specific document for exact search (optional)</param>
        /// <param name="page">Page number (starts at 1)</param>
        /// <param name="pageSize">Page size</param>
        /// <response code="200">List of customers found</response>
        /// <response code="400">Invalid pagination parameters</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to access the resource</response>
        [HttpGet("Search")]
        [ProducesResponseType(typeof(CustomerSearchResponse), 200)]
        public async Task<IActionResult> SearchCustomersAsync(
            [FromQuery] string term = "",
            [FromQuery] string document = "",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest("Page number and page size must be greater than zero.");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            try
            {
                // Verify if it's a document search
                if (!string.IsNullOrWhiteSpace(document))
                {
                    // Create cache key for document search
                    string documentCacheKey = $"customer_document:{groupId}:{document}";
                    
                    // Try to get from cache first
                    var cachedDocumentBytes = await _distributedCache.GetAsync(documentCacheKey);
                    if (cachedDocumentBytes != null)
                    {
                        try
                        {
                            // Deserialize cached result
                            var cachedResultJson = Encoding.UTF8.GetString(cachedDocumentBytes);
                            var cachedCustomer = JsonSerializer.Deserialize<CustomerDto>(
                                cachedResultJson,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (cachedCustomer != null)
                            {
                                var cachedResponse = new CustomerSearchResponse
                                {
                                    Items = new List<CustomerDto> { cachedCustomer },
                                    TotalCount = 1,
                                    Page = 1,
                                    PageSize = 1,
                                    Pages = 1,
                                    FromCache = true
                                };
                                return Ok(cachedResponse);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error deserializing cached customer by document");
                            // Continue to search in the database in case of error
                        }
                    }

                    // If not in cache or error, search in the database
                    var customer = await _customerRepository.GetByDocumentAsync(document, groupId);
                    if (customer != null)
                    {
                        var customerDto = _mapper.Map<CustomerDto>(customer);
                        
                        // Store in cache
                        try
                        {
                            var serializedCustomer = JsonSerializer.Serialize(customerDto);
                            var cacheOptions = new DistributedCacheEntryOptions()
                                .SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));

                            await _distributedCache.SetAsync(
                                documentCacheKey,
                                Encoding.UTF8.GetBytes(serializedCustomer),
                                cacheOptions);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error caching customer by document");
                            // Keep going without cache in case of error
                        }

                        var response = new CustomerSearchResponse
                        {
                            Items = new List<CustomerDto> { customerDto },
                            TotalCount = 1,
                            Page = 1,
                            PageSize = 1,
                            Pages = 1,
                            FromCache = false
                        };
                        return Ok(response);
                    }
                    else
                    {
                        // Document not found
                        return Ok(new CustomerSearchResponse
                        {
                            Items = new List<CustomerDto>(),
                            TotalCount = 0,
                            Page = 1,
                            PageSize = 1,
                            Pages = 0,
                            FromCache = false
                        });
                    }
                }

                // If it's not a document search, check if it's a search without term
                if (string.IsNullOrWhiteSpace(term))
                {
                    // Generate cache key for all customers
                    string allCustomersCacheKey = $"all_customers:{groupId}:{page}:{pageSize}";

                    // Try to get from cache first
                    var cachedResultBytes = await _distributedCache.GetAsync(allCustomersCacheKey);
                    if (cachedResultBytes != null)
                    {
                        try
                        {
                            // Deserialize cached result
                            var cachedResultJson = Encoding.UTF8.GetString(cachedResultBytes);
                            var cachedResponse = JsonSerializer.Deserialize<CustomerSearchResponse>(
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
                            _logger.LogError(ex, "Error deserializing cached all customers result");
                            // Keep going without cache in case of error
                        }
                    }

                    // If not in cache or error, search in the database
                    var (dbItems, dbTotalCount) = await _customerRepository.SearchAsync("", groupId, page, pageSize);
                    var dbDtos = dbItems.Select(c => _mapper.Map<CustomerDto>(c)).ToList();

                    var dbResponse = new CustomerSearchResponse
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
                            allCustomersCacheKey,
                            Encoding.UTF8.GetBytes(serializedResponse),
                            cacheOptions);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error caching all customers result");
                        // Keep going without cache in case of error
                    }
                    
                    return Ok(dbResponse);
                }

                // Search by term (name or part of the document)
                // Generate cache key for the search context of this user
                string searchesMapKey = $"customer_searches:{currentUser.Id}:{groupId}";

                // Try to get the previous search map from Redis
                Dictionary<string, CachedCustomerSearch>? userSearchCache = null;
                var cachedSearchesBytes = await _distributedCache.GetAsync(searchesMapKey);

                if (cachedSearchesBytes != null)
                {
                    // Deserialize the previous search map from Redis
                    var cachedSearchesJson = Encoding.UTF8.GetString(cachedSearchesBytes);
                    userSearchCache = JsonSerializer.Deserialize<Dictionary<string, CachedCustomerSearch>?>(
                        cachedSearchesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (userSearchCache != null)
                    {
                        // Find a previous search that is a prefix of the current search
                        foreach (var kvp in userSearchCache.OrderByDescending(k => k.Key.Length))
                        {
                            if (term.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase) && 
                                kvp.Value.Page == page && 
                                kvp.Value.PageSize == pageSize)
                            {
                                // Found a cached result that is a prefix of this search
                                var cachedResponse = new CustomerSearchResponse
                                {
                                    Items = kvp.Value.Items,
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
                var (searchItems, searchTotalCount) = await _customerRepository.SearchAsync(term, groupId, page, pageSize);
                var searchDtos = searchItems.Select(c => _mapper.Map<CustomerDto>(c)).ToList();

                var searchResponse = new CustomerSearchResponse
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
                    userSearchCache ??= new Dictionary<string, CachedCustomerSearch>();

                    // Add or update this search in the cache
                    userSearchCache[term] = new CachedCustomerSearch
                    {
                        Items = searchDtos,
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
                    _logger.LogError(ex, "Error updating customer search cache");
                    // Keep going without cache in case of error
                }

                return Ok(searchResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers");
                return StatusCode(500, "An error occurred while searching customers. Please try again.");
            }
        }

        /// <summary>
        /// Get a customer by ID
        /// </summary>
        /// <remarks>
        /// This endpoint returns the details of a specific customer.
        /// All users with group access can view customers.
        /// </remarks>
        /// <param name="id">Customer ID</param>
        /// <response code="200">Customer details</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to access the resource</response>
        /// <response code="404">Customer not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        public async Task<IActionResult> GetCustomerByIdAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            try
            {
                var customer = await _customerRepository.GetByIdAsync(id, groupId);
                if (customer == null) return NotFound();

                var customerDto = _mapper.Map<CustomerDto>(customer);
                return Ok(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by ID");
                return StatusCode(500, "An error occurred while retrieving the customer. Please try again.");
            }
        }

        /// <summary>
        /// Create a new customer
        /// </summary>
        /// <remarks>
        /// This endpoint creates a new customer with the following validations:
        /// - Checks if the user has Transaction or Promotion permission
        /// - Validates required fields and formats
        /// 
        /// Only users with Transaction or Promotion permission can create customers.
        /// </remarks>
        /// <param name="model">Customer data to be created</param>
        /// <response code="201">Customer created successfully</response>
        /// <response code="400">Invalid data</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to create customers</response>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerDto), 201)]
        public async Task<IActionResult> CreateCustomerAsync([FromBody] CreateCustomerRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to create customers. You need Transaction or Promotion permission.");
            }

            try
            {
                // Check if a customer with the same document already exists in this group
                var existingCustomer = await _customerRepository.GetByDocumentAsync(model.Document, groupId);
                if (existingCustomer != null)
                {
                    return BadRequest($"A customer with document '{model.Document}' already exists in this group.");
                }
                
                var customer = await _customerRepository.CreateCustomerAsync(
                    groupId: groupId,
                    name: model.Name,
                    document: model.Document,
                    email: model.Email,
                    phone: model.Phone,
                    address: model.Address
                );

                var customerDto = _mapper.Map<CustomerDto>(customer);
                return CreatedAtAction(nameof(GetCustomerByIdAsync), new { id = customer.Id }, customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return StatusCode(500, "An error occurred while creating the customer. Please try again.");
            }
        }

        /// <summary>
        /// Update an existing customer
        /// </summary>
        /// <remarks>
        /// This endpoint updates an existing customer with the following validations:
        /// - Checks if the customer exists and belongs to the user's group
        /// - Checks if the user has Transaction or Promotion permission
        /// - Validates required fields and formats
        /// 
        /// Only users with Transaction or Promotion permission can update customers.
        /// </remarks>
        /// <param name="model">Customer data to be updated</param>
        /// <response code="200">Customer updated successfully</response>
        /// <response code="400">Invalid data</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to update customers</response>
        /// <response code="404">Customer not found</response>
        [HttpPut]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        public async Task<IActionResult> UpdateCustomerAsync([FromBody] UpdateCustomerRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to update customers. You need Transaction or Promotion permission.");
            }

            try
            {
                // Verify if the customer exists and belongs to the user's group
                var existingCustomer = await _customerRepository.GetByIdAsync(model.Id, groupId);
                if (existingCustomer == null) return NotFound();
                
                // Check if another customer with the same document already exists in this group
                if (existingCustomer.Document != model.Document)
                {
                    var customerWithSameDocument = await _customerRepository.GetByDocumentAsync(model.Document, groupId);
                    if (customerWithSameDocument != null)
                    {
                        return BadRequest($"Another customer with document '{model.Document}' already exists in this group.");
                    }
                }

                var customer = await _customerRepository.UpdateCustomerAsync(
                    id: model.Id,
                    groupId: groupId,
                    name: model.Name,
                    document: model.Document,
                    email: model.Email,
                    phone: model.Phone,
                    address: model.Address
                );

                if (customer == null) return NotFound();

                var customerDto = _mapper.Map<CustomerDto>(customer);
                return Ok(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer");
                return StatusCode(500, "An error occurred while updating the customer. Please try again.");
            }
        }

        /// <summary>
        /// Delete a customer by ID
        /// </summary>
        /// <remarks>
        /// This endpoint deletes a customer with the following validations:
        /// - Checks if the customer exists and belongs to the user's group
        /// - Checks if the user has Transaction or Promotion permission
        /// 
        /// Only users with Transaction or Promotion permission can delete customers.
        /// </remarks>
        /// <param name="id">ID of the customer to be deleted</param>
        /// <response code="200">Customer deleted successfully</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to delete customers</response>
        /// <response code="404">Customer not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        public async Task<IActionResult> DeleteCustomerAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to delete customers. You need Transaction or Promotion permission.");
            }

            try
            {
                // Verify if the customer exists and belongs to the user's group
                var existingCustomer = await _customerRepository.GetByIdAsync(id, groupId);
                if (existingCustomer == null) return NotFound();

                var customer = await _customerRepository.DeleteCustomerAsync(id, groupId);
                if (customer == null) return NotFound();

                var customerDto = _mapper.Map<CustomerDto>(customer);
                return Ok(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer");
                return StatusCode(500, "An error occurred while deleting the customer. Please try again.");
            }
        }

        /// <summary>
        /// Activate a customer
        /// </summary>
        /// <remarks>
        /// This endpoint activates an inactive customer with the following validations:
        /// - Checks if the customer exists and belongs to the user's group
        /// - Checks if the customer is already active
        /// - Checks if the user has Transaction or Promotion permission
        /// 
        /// Only users with Transaction or Promotion permission can activate customers.
        /// </remarks>
        /// <param name="id">ID of the customer to be activated</param>
        /// <response code="200">Customer activated successfully</response>
        /// <response code="400">Customer is already active</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to activate customers</response>
        /// <response code="404">Customer not found</response>
        [HttpPatch("{id}/Activate")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        public async Task<IActionResult> ActivateCustomerAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to activate customers. You need Transaction or Promotion permission.");
            }

            try
            {
                // Verify if the customer exists and belongs to the user's group
                var existingCustomer = await _customerRepository.GetByIdAsync(id, groupId);
                if (existingCustomer == null) return NotFound();

                // Verify if the customer is already active
                if (existingCustomer.Active)
                {
                    return BadRequest("This customer is already active.");
                }

                var customer = await _customerRepository.ActivateCustomerAsync(id, groupId);
                if (customer == null) return NotFound();

                var customerDto = _mapper.Map<CustomerDto>(customer);
                return Ok(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating customer");
                return StatusCode(500, "An error occurred while activating the customer. Please try again.");
            }
        }

        /// <summary>
        /// Deactivate a customer
        /// </summary>
        /// <remarks>
        /// This endpoint deactivates an active customer with the following validations:
        /// - Checks if the customer exists and belongs to the user's group
        /// - Checks if the customer is already inactive
        /// - Checks if the user has Transaction or Promotion permission
        /// 
        /// Only users with Transaction or Promotion permission can deactivate customers.
        /// </remarks>
        /// <param name="id">ID of the customer to be deactivated</param>
        /// <response code="200">Customer deactivated successfully</response>
        /// <response code="400">Customer is already inactive</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to deactivate customers</response>
        /// <response code="404">Customer not found</response>
        [HttpPatch("{id}/Deactivate")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        public async Task<IActionResult> DeactivateCustomerAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to deactivate customers. You need Transaction or Promotion permission.");
            }

            try
            {
                // Verify if the customer exists and belongs to the user's group
                var existingCustomer = await _customerRepository.GetByIdAsync(id, groupId);
                if (existingCustomer == null) return NotFound();

                // Verify if the customer is already inactive
                if (!existingCustomer.Active)
                {
                    return BadRequest("This customer is already inactive.");
                }

                var customer = await _customerRepository.DeactivateCustomerAsync(id, groupId);
                if (customer == null) return NotFound();

                var customerDto = _mapper.Map<CustomerDto>(customer);
                return Ok(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating customer");
                return StatusCode(500, "An error occurred while deactivating the customer. Please try again.");
            }
        }

        /// <summary>
        /// Link a customer to a loyalty program
        /// </summary>
        /// <remarks>
        /// This endpoint links a customer to a loyalty program with the following validations:
        /// - Checks if the customer exists and belongs to the user's group
        /// - Checks if the loyalty program exists and belongs to the user's group
        /// - Checks if the user has Transaction or Promotion permission
        /// 
        /// Only users with Transaction or Promotion permission can link customers to loyalty programs.
        /// </remarks>
        /// <param name="model">Data to link customer to loyalty program</param>
        /// <response code="200">Customer linked successfully</response>
        /// <response code="400">Customer is already linked to a program</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to link customers</response>
        /// <response code="404">Customer or loyalty program not found</response>
        [HttpPost("AttachToLoyaltyProgram")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        public async Task<IActionResult> LinkToLoyaltyProgramAsync([FromBody] LinkLoyaltyProgramRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to link customers to loyalty programs. You need Transaction or Promotion permission.");
            }

            try
            {
                // Verify if the customer exists and belongs to the user's group
                var existingCustomer = await _customerRepository.GetByIdAsync(model.CustomerId, groupId);
                if (existingCustomer == null) return NotFound("Customer not found.");

                // Verify if the customer is already linked to a loyalty program
                if (!string.IsNullOrEmpty(existingCustomer.LoyaltyProgramId))
                {
                    return BadRequest("This customer is already linked to a loyalty program.");
                }

                if (!existingCustomer.Active)
                {
                    return BadRequest("Cannot attach loyalty program to inactive customer.");
                }

                // Verify if the loyalty program exists and belongs to the user's group
                var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(model.LoyaltyProgramId, groupId);
                if (loyaltyProgram == null) return NotFound("Loyalty program not found.");
                
                // Verify if the loyalty program is active
                if (!loyaltyProgram.IsActive)
                {
                    return BadRequest($"Cannot link customer to loyalty program '{loyaltyProgram.Name}' because it is inactive.");
                }

                var customer = await _customerRepository.LinkToLoyaltyProgramAsync(
                    id: model.CustomerId,
                    groupId: groupId,
                    loyaltyProgramId: model.LoyaltyProgramId);

                if (customer == null) return NotFound();

                var customerDto = _mapper.Map<CustomerDto>(customer);
                return Ok(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking customer to loyalty program");
                return StatusCode(500, "An error occurred while linking the customer to the loyalty program. Please try again.");
            }
        }

        /// <summary>
        /// Unlink a customer from a loyalty program
        /// </summary>
        /// <remarks>
        /// This endpoint unlinks a customer from a loyalty program with the following validations:
        /// - Checks if the customer exists and belongs to the user's group
        /// - Checks if the customer is linked to a loyalty program
        /// - Checks if the customer has no points in the program
        /// - Checks if the user has Transaction or Promotion permission
        /// 
        /// Only users with Transaction or Promotion permission can unlink customers from loyalty programs.
        /// </remarks>
        /// <param name="id">ID of the customer to be unlinked</param>
        /// <response code="200">Customer unlinked successfully</response>
        /// <response code="400">Customer has points or is not linked to a program</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to unlink customers</response>
        /// <response code="404">Customer not found</response>
        [HttpPost("{id}/DetachFromLoyaltyProgram")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        public async Task<IActionResult> UnlinkFromLoyaltyProgramAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to unlink customers from loyalty programs. You need Transaction or Promotion permission.");
            }

            try
            {
                // Verify if the customer exists and belongs to the user's group
                var existingCustomer = await _customerRepository.GetByIdAsync(id, groupId);
                if (existingCustomer == null) return NotFound("Customer not found.");

                // Verify if the customer is linked to a loyalty program
                if (string.IsNullOrEmpty(existingCustomer.LoyaltyProgramId))
                {
                    return BadRequest("This customer is not linked to any loyalty program.");
                }

                // Verify if the customer has points
                if (existingCustomer.LoyaltyPoints > 0)
                {
                    return BadRequest($"Cannot unlink customer from loyalty program. Customer has {existingCustomer.LoyaltyPoints} points remaining. Points must be zero before unlinking.");
                }

                var customer = await _customerRepository.UnlinkFromLoyaltyProgramAsync(id, groupId);
                if (customer == null) return NotFound();

                var customerDto = _mapper.Map<CustomerDto>(customer);
                return Ok(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking customer from loyalty program");
                return StatusCode(500, "An error occurred while unlinking the customer from the loyalty program. Please try again.");
            }
        }

        /// <summary>
        /// Add points to a customer following the loyalty program rule
        /// </summary>
        /// <remarks>
        /// This endpoint adds points to a customer with the following validations:
        /// - Checks if the customer exists and belongs to the user's group
        /// - Checks if the customer is linked to a loyalty program
        /// - Checks if the customer is active
        /// - Checks if the user has Transaction permission
        /// - Applies the loyalty program's point conversion rule
        /// 
        /// Only users with Transaction permission can add points to customers.
        /// </remarks>
        /// <param name="model">Data to add points</param>
        /// <response code="200">Points added successfully</response>
        /// <response code="400">Customer inactive or not linked to a program</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to add points</response>
        /// <response code="404">Customer not found</response>
        [HttpPost("AddPoints")]
        [ProducesResponseType(typeof(CustomerPointsResponse), 200)]
        public async Task<IActionResult> AddPointsAsync([FromBody] AddPointsRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            // Check if the user has permission to modify customers (requires Transaction permission)
            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to add points to customers. You need Transaction permission.");
            }

            try
            {
                // Verify if the customer exists and belongs to the user's group
                var customer = await _customerRepository.GetByIdAsync(model.CustomerId, groupId);
                if (customer == null) return NotFound("Customer not found.");

                // Verify if the customer is active
                if (!customer.Active)
                {
                    return BadRequest("Cannot add points to inactive customer.");
                }

                // Verify if the customer is linked to a loyalty program
                if (string.IsNullOrEmpty(customer.LoyaltyProgramId))
                {
                    return BadRequest("Customer is not linked to any loyalty program.");
                }

                // Get the loyalty program
                var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
                if (loyaltyProgram == null)
                {
                    return BadRequest("The loyalty program linked to this customer no longer exists.");
                }

                // Apply the loyalty program's point conversion rule
                decimal amount = model.Amount;
                // Corrigindo o cálculo para dividir pelo valor da taxa de conversão
                int pointsToAdd = (int)Math.Floor(amount * 100 / loyaltyProgram.CentsToPoints);

                if (pointsToAdd <= 0)
                {
                    return BadRequest($"The amount {amount:C} is too small to generate points with the current conversion rate (1 point per {loyaltyProgram.CentsToPoints/100} currency units).");
                }

                // Add points
                var updatedCustomer = await _customerRepository.AddPointsAsync(
                    id: model.CustomerId,
                    groupId: groupId,
                    points: pointsToAdd,
                    description: model.Description ?? $"Purchase: {amount:C}");

                if (updatedCustomer == null) return NotFound();

                var response = new CustomerPointsResponse
                {
                    CustomerId = updatedCustomer.Id,
                    CustomerName = updatedCustomer.Name,
                    LoyaltyProgramId = updatedCustomer.LoyaltyProgramId,
                    LoyaltyProgramName = loyaltyProgram.Name,
                    PointsAdded = pointsToAdd,
                    CurrentPoints = updatedCustomer.LoyaltyPoints,
                    ConversionRate = loyaltyProgram.CentsToPoints,
                    TransactionAmount = amount,
                    TransactionDescription = model.Description ?? $"Purchase: {amount:C}",
                    TransactionDate = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding points to customer");
                return StatusCode(500, "An error occurred while adding points to the customer. Please try again.");
            }
        }

        /// <summary>
        /// Remove points from a customer following the loyalty program rule
        /// </summary>
        /// <remarks>
        /// This endpoint removes points from a customer with the following validations:
        /// - Checks if the customer exists and belongs to the user's group
        /// - Checks if the customer is linked to a loyalty program
        /// - Checks if the customer is active
        /// - Checks if the user has Transaction permission
        /// - Applies the loyalty program's point conversion rule
        /// - Checks if the customer has enough points
        /// 
        /// Only users with Transaction permission can remove points from customers.
        /// </remarks>
        /// <param name="model">Data to remove points</param>
        /// <response code="200">Points removed successfully</response>
        /// <response code="400">Customer inactive, not linked to a program, or insufficient points</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to remove points</response>
        /// <response code="404">Customer not found</response>
        [HttpPost("RemovePoints")]
        [ProducesResponseType(typeof(CustomerPointsResponse), 200)]
        public async Task<IActionResult> RemovePointsAsync([FromBody] RemovePointsRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            // Check if the user has permission to modify customers (requires Transaction permission)
            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to remove points from customers. You need Transaction permission.");
            }
            
            try
            {
                // Verify if the customer exists and belongs to the user's group
                var customer = await _customerRepository.GetByIdAsync(model.CustomerId, groupId);
                if (customer == null) return NotFound("Customer not found.");

                // Verify if the customer is active
                if (!customer.Active)
                {
                    return BadRequest("Cannot remove points from inactive customer.");
                }

                // Verify if the customer is linked to a loyalty program
                if (string.IsNullOrEmpty(customer.LoyaltyProgramId))
                {
                    return BadRequest("Customer is not linked to any loyalty program.");
                }

                // Get the loyalty program
                var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
                if (loyaltyProgram == null)
                {
                    return BadRequest("The loyalty program linked to this customer no longer exists.");
                }

                // Apply the loyalty program's point conversion rule
                decimal amount = model.Amount;
                // Corrigindo o cálculo para dividir pelo valor da taxa de conversão
                int pointsToRemove = (int)Math.Floor(amount * 100 / loyaltyProgram.CentsToPoints);

                if (pointsToRemove <= 0)
                {
                    return BadRequest($"The amount {amount:C} is too small to calculate points with the current conversion rate (1 point per {loyaltyProgram.CentsToPoints/100} currency units).");
                }

                // Verify if the customer has enough points
                if (customer.LoyaltyPoints < pointsToRemove)
                {
                    return BadRequest($"Customer does not have enough points. Required: {pointsToRemove}, Available: {customer.LoyaltyPoints}");
                }

                // Redeem points
                var updatedCustomer = await _customerRepository.RemovePointsAsync(
                    id: model.CustomerId,
                    groupId: groupId,
                    points: pointsToRemove,
                    description: model.Description ?? $"Redemption: {amount:C}");

                if (updatedCustomer == null) return NotFound();

                var response = new CustomerPointsResponse
                {
                    CustomerId = updatedCustomer.Id,
                    CustomerName = updatedCustomer.Name,
                    LoyaltyProgramId = updatedCustomer.LoyaltyProgramId,
                    LoyaltyProgramName = loyaltyProgram.Name,
                    PointsAdded = -pointsToRemove, // Valor negativo para indicar retirada
                    CurrentPoints = updatedCustomer.LoyaltyPoints,
                    ConversionRate = loyaltyProgram.CentsToPoints,
                    TransactionAmount = amount,
                    TransactionDescription = model.Description ?? $"Redemption: {amount:C}",
                    TransactionDate = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing points from customer");
                return StatusCode(500, "An error occurred while removing points from the customer. Please try again.");
            }
        }

        /// <summary>
        /// Administratively remove points from a customer without following program rules
        /// </summary>
        /// <remarks>
        /// This endpoint removes points from a customer without applying conversion rules, with the following validations:
        /// - Checks if the customer exists and belongs to the user's group
        /// - Checks if the customer is linked to a loyalty program
        /// - Checks if the customer is active
        /// - Checks if the user has Promotion permission
        /// - Checks if the customer has enough points
        /// 
        /// Only users with Promotion permission can administratively remove points.
        /// </remarks>
        /// <param name="model">Data to administratively remove points</param>
        /// <response code="200">Points removed successfully</response>
        /// <response code="400">Customer inactive, not linked to a program, or insufficient points</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to administratively remove points</response>
        /// <response code="404">Customer not found</response>
        [HttpPost("AdminRemovePoints")]
        [ProducesResponseType(typeof(CustomerPointsResponse), 200)]
        public async Task<IActionResult> AdminRemovePointsAsync([FromBody] AdminRemovePointsRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            // Check if the user has permission to modify customers (requires Promotion permission)
            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to administratively remove points from customers. You need Promotion permission.");
            }
        

            try
            {
                // Verify if the customer exists and belongs to the user's group
                var customer = await _customerRepository.GetByIdAsync(model.CustomerId, groupId);
                if (customer == null) return NotFound("Customer not found.");

                // Verify if the customer is active
                if (!customer.Active)
                {
                    return BadRequest("Cannot remove points from inactive customer.");
                }

                // Verify if the customer is linked to a loyalty program
                if (string.IsNullOrEmpty(customer.LoyaltyProgramId))
                {
                    return BadRequest("Customer is not linked to any loyalty program.");
                }

                // Get the loyalty program
                var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
                if (loyaltyProgram == null)
                {
                    return BadRequest("The loyalty program linked to this customer no longer exists.");
                }

                int pointsToRemove = model.Points;

                // Verify if the customer has enough points
                if (customer.LoyaltyPoints < pointsToRemove)
                {
                    return BadRequest($"Customer does not have enough points. Required: {pointsToRemove}, Available: {customer.LoyaltyPoints}");
                }

                // Redeem points
                var updatedCustomer = await _customerRepository.RemovePointsAsync(
                    id: model.CustomerId,
                    groupId: groupId,
                    points: pointsToRemove,
                    description: $"Administrative removal: {model.Description}");

                if (updatedCustomer == null) return NotFound();

                var response = new CustomerPointsResponse
                {
                    CustomerId = updatedCustomer.Id,
                    CustomerName = updatedCustomer.Name,
                    LoyaltyProgramId = updatedCustomer.LoyaltyProgramId,
                    LoyaltyProgramName = loyaltyProgram.Name,
                    PointsAdded = -pointsToRemove, // Value negative to indicate withdrawal
                    CurrentPoints = updatedCustomer.LoyaltyPoints,
                    ConversionRate = loyaltyProgram.CentsToPoints,
                    TransactionAmount = 0, // No transaction amount for administrative withdrawal
                    TransactionDescription = $"Administrative removal: {model.Description}",
                    TransactionDate = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error administratively removing points from customer");
                return StatusCode(500, "An error occurred while removing points from the customer. Please try again.");
            }
        }

        /// <summary>
        /// Query a customer's points balance
        /// </summary>
        /// <remarks>
        /// This endpoint queries a customer's points balance with the following validations:
        /// - Checks if the customer exists and belongs to the user's group
        /// - Checks if the customer is linked to a loyalty program
        /// 
        /// Any authenticated user can query the points balance of customers in their group.
        /// </remarks>
        /// <param name="id">Customer ID</param>
        /// <response code="200">Points balance queried successfully</response>
        /// <response code="400">Customer not linked to a program</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="404">Customer not found</response>
        [HttpGet("{id}/Points")]
        [ProducesResponseType(typeof(CustomerPointsResponse), 200)]
        public async Task<IActionResult> GetPointsAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            // Check if the user has permission to access customers
            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to view customer points.");
            }

            try
            {
                // Check if the customer exists and belongs to the user's group
                var customer = await _customerRepository.GetByIdAsync(id, groupId);
                if (customer == null) return NotFound("Customer not found.");
                
                // Verify if the customer is active
                if (!customer.Active)
                {
                    return BadRequest("Cannot view points for inactive customer.");
                }

                // Check if the customer is linked to a loyalty program
                if (string.IsNullOrEmpty(customer.LoyaltyProgramId))
                {
                    return BadRequest("Customer is not linked to any loyalty program.");
                }

                // Get the loyalty program
                var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
                if (loyaltyProgram == null)
                {
                    return BadRequest("The loyalty program linked to this customer no longer exists.");
                }

                var response = new CustomerPointsResponse
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    LoyaltyProgramId = customer.LoyaltyProgramId,
                    LoyaltyProgramName = loyaltyProgram.Name,
                    PointsAdded = 0, // Query doesn't add or remove points
                    CurrentPoints = customer.LoyaltyPoints,
                    ConversionRate = loyaltyProgram.CentsToPoints,
                    TransactionAmount = 0, // No transaction value for query
                    TransactionDescription = "Points balance inquiry",
                    TransactionDate = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer points");
                return StatusCode(500, "An error occurred while getting customer points. Please try again.");
            }
        }

        /// <summary>
        /// Checks if the user has permission to access customers
        /// </summary>
        private async Task<(bool hasPermission, string groupId)> CheckPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                // For viewing, all users have permission
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

        /// <summary>
        /// Checks if the user has permission to modify customers (Transaction or Promotion)
        /// </summary>
        private async Task<(bool hasPermission, string groupId)> CheckPermissionForCUDAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                // Check if the user has Transaction or Promotion permission
                bool hasTransactionPermission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Transaction);
                bool hasPromotionPermission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Promotion);
                
                if (!hasTransactionPermission && !hasPromotionPermission)
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
