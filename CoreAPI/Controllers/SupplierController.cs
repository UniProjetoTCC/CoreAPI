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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreAPI.Controllers
{
    /// <summary>
    /// Controller responsible for managing suppliers.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<SupplierController> _logger;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _distributedCache;
        private readonly ILinkedUserService _linkedUserService;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly int cacheDurationMinutes = 15;

        public SupplierController(
            ISupplierRepository supplierRepository,
            UserManager<IdentityUser> userManager,
            ILogger<SupplierController> logger,
            IMapper mapper,
            IDistributedCache distributedCache,
            ILinkedUserService linkedUserService,
            ILinkedUserRepository linkedUserRepository,
            IUserGroupRepository userGroupRepository)
        {
            _supplierRepository = supplierRepository;
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
            _distributedCache = distributedCache;
            _linkedUserService = linkedUserService;
            _linkedUserRepository = linkedUserRepository;
            _userGroupRepository = userGroupRepository;
        }

        /// <summary>
        /// Search suppliers by name or document
        /// </summary>
        /// <remarks>
        /// This endpoint allows searching suppliers by name or document with pagination and caching.
        /// All users with group access can view suppliers.
        /// </remarks>
        /// <param name="term">Search term (name or document) (optional)</param>
        /// <param name="page">Page number (starts at 1)</param>
        /// <param name="pageSize">Page size</param>
        /// <response code="200">List of suppliers found</response>
        /// <response code="400">Invalid pagination parameters</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to access the resource</response>
        [HttpGet("Search")]
        [ProducesResponseType(typeof(SupplierSearchResponse), 200)]
        public async Task<IActionResult> SearchSuppliersAsync(
            [FromQuery] string term = "",
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
                return StatusCode(403, "You don't have permission to access this resource.");
            }

            try
            {
                // Generate cache key
                string cacheKey = $"suppliers:{groupId}:{term}:{page}:{pageSize}";
                var cachedResultBytes = await _distributedCache.GetAsync(cacheKey);

                if (cachedResultBytes != null)
                {
                    try
                    {
                        var cachedResultJson = Encoding.UTF8.GetString(cachedResultBytes);
                        var cachedResponse = JsonSerializer.Deserialize<SupplierSearchResponse>(
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
                        _logger.LogError(ex, "Error deserializing cached supplier search result");
                    }
                }

                // If not in cache, get from database
                var (dbItems, dbTotalCount) = await _supplierRepository.SearchAsync(groupId, term, page, pageSize);
                var dbDtos = _mapper.Map<List<SupplierDto>>(dbItems);

                var dbResponse = new SupplierSearchResponse
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
                        cacheKey,
                        Encoding.UTF8.GetBytes(serializedResponse),
                        cacheOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error caching supplier search result");
                }

                return Ok(dbResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching suppliers");
                return StatusCode(500, "An error occurred while searching suppliers.");
            }
        }

        /// <summary>
        /// Get a supplier by ID
        /// </summary>
        /// <param name="id">Supplier ID</param>
        /// <response code="200">Supplier details</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to access the resource</response>
        /// <response code="404">Supplier not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SupplierDto), 200)]
        public async Task<IActionResult> GetSupplierById(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource.");
            }

            try
            {
                var supplier = await _supplierRepository.GetByIdAsync(id, groupId);
                if (supplier == null) return NotFound();

                var supplierDto = _mapper.Map<SupplierDto>(supplier);
                return Ok(supplierDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier by ID");
                return StatusCode(500, "An error occurred while retrieving the supplier.");
            }
        }

        /// <summary>
        /// Create a new supplier
        /// </summary>
        /// <remarks>
        /// Requires Product permission.
        /// </remarks>
        /// <param name="model">Supplier data to be created</param>
        /// <response code="201">Supplier created successfully</response>
        /// <response code="400">Invalid data or duplicate document/email</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to create suppliers</response>
        [HttpPost]
        [ProducesResponseType(typeof(SupplierDto), 201)]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to create suppliers. You need Product permission.");
            }

            try
            {
                // Check for duplicates
                var existingByDoc = await _supplierRepository.GetByDocumentAsync(model.Document, groupId);
                if (existingByDoc != null)
                {
                    return BadRequest($"A supplier with document '{model.Document}' already exists.");
                }

                var existingByEmail = await _supplierRepository.GetByEmailAsync(model.Email, groupId);
                if (existingByEmail != null)
                {
                    return BadRequest($"A supplier with email '{model.Email}' already exists.");
                }

                var supplierBusinessModel = _mapper.Map<SupplierBusinessModel>(model);
                supplierBusinessModel.GroupId = groupId; // Assign group ID

                var createdSupplier = await _supplierRepository.CreateAsync(supplierBusinessModel);

                var supplierDto = _mapper.Map<SupplierDto>(createdSupplier);
                return CreatedAtAction(nameof(GetSupplierById), new { id = supplierDto.Id }, supplierDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                return StatusCode(500, "An error occurred while creating the supplier.");
            }
        }

        /// <summary>
        /// Update an existing supplier
        /// </summary>
        /// <remarks>
        /// Requires Product permission.
        /// </remarks>
        /// <param name="id">ID of the supplier to update</param>
        /// <param name="model">Supplier data to be updated</param>
        /// <response code="200">Supplier updated successfully</response>
        /// <response code="400">Invalid data or duplicate document/email</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to update suppliers</response>
        /// <response code="404">Supplier not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(SupplierDto), 200)]
        public async Task<IActionResult> UpdateSupplierAsync(string id, [FromBody] UpdateSupplierRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to update suppliers. You need Product permission.");
            }

            try
            {
                var existingSupplier = await _supplierRepository.GetByIdAsync(id, groupId);
                if (existingSupplier == null) return NotFound();

                // Check for duplicates if email/document changed
                if (existingSupplier.Document != model.Document)
                {
                    var existingByDoc = await _supplierRepository.GetByDocumentAsync(model.Document, groupId);
                    if (existingByDoc != null)
                    {
                        return BadRequest($"Another supplier with document '{model.Document}' already exists.");
                    }
                }
                
                if (existingSupplier.Email.ToLower() != model.Email.ToLower())
                {
                    var existingByEmail = await _supplierRepository.GetByEmailAsync(model.Email, groupId);
                    if (existingByEmail != null)
                    {
                        return BadRequest($"Another supplier with email '{model.Email}' already exists.");
                    }
                }

                var supplierBusinessModel = _mapper.Map<SupplierBusinessModel>(model);
                var updatedSupplier = await _supplierRepository.UpdateAsync(id, groupId, supplierBusinessModel);

                if (updatedSupplier == null) return NotFound();

                var supplierDto = _mapper.Map<SupplierDto>(updatedSupplier);
                return Ok(supplierDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier");
                return StatusCode(500, "An error occurred while updating the supplier.");
            }
        }

        /// <summary>
        /// Activate a supplier
        /// </summary>
        /// <remarks>
        /// Requires Product permission.
        /// </remarks>
        /// <param name="id">ID of the supplier to be activated</param>
        /// <response code="200">Supplier activated successfully</response>
        /// <response code="400">Supplier is already active</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to activate</response>
        /// <response code="404">Supplier not found</response>
        [HttpPatch("{id}/Activate")]
        [ProducesResponseType(typeof(SupplierDto), 200)]
        public async Task<IActionResult> ActivateSupplierAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to activate suppliers. You need Product permission.");
            }

            try
            {
                var existingSupplier = await _supplierRepository.GetByIdAsync(id, groupId);
                if (existingSupplier == null) return NotFound();

                if (existingSupplier.IsActive)
                {
                    return BadRequest("This supplier is already active.");
                }

                var supplier = await _supplierRepository.ActivateAsync(id, groupId);
                if (supplier == null) return NotFound();

                var supplierDto = _mapper.Map<SupplierDto>(supplier);
                return Ok(supplierDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating supplier");
                return StatusCode(500, "An error occurred while activating the supplier.");
            }
        }

        /// <summary>
        /// Deactivate a supplier
        /// </summary>
        /// <remarks>
        /// Requires Product permission.
        /// </remarks>
        /// <param name="id">ID of the supplier to be deactivated</param>
        /// <response code="200">Supplier deactivated successfully</response>
        /// <response code="400">Supplier is already inactive</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="403">No permission to deactivate</response>
        /// <response code="404">Supplier not found</response>
        [HttpPatch("{id}/Deactivate")]
        [ProducesResponseType(typeof(SupplierDto), 200)]
        public async Task<IActionResult> DeactivateSupplierAsync(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to deactivate suppliers. You need Product permission.");
            }

            try
            {
                var existingSupplier = await _supplierRepository.GetByIdAsync(id, groupId);
                if (existingSupplier == null) return NotFound();

                if (!existingSupplier.IsActive)
                {
                    return BadRequest("This supplier is already inactive.");
                }
                
                // TODO: Adicionar verificação de dependências (ex: Pedidos de Compra abertos) antes de desativar

                var supplier = await _supplierRepository.DeactivateAsync(id, groupId);
                if (supplier == null) return NotFound();

                var supplierDto = _mapper.Map<SupplierDto>(supplier);
                return Ok(supplierDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating supplier");
                return StatusCode(500, "An error occurred while deactivating the supplier.");
            }
        }


        // --- Métodos de Permissão ---

        /// <summary>
        /// Checks read permission (all users in group)
        /// </summary>
        private async Task<(bool hasPermission, string groupId)> CheckPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                // All linked users can view suppliers
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
                groupId = linkedUser?.GroupId ?? string.Empty;
            }
            else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(userId);
                groupId = group?.GroupId ?? string.Empty;
            }

            return (!string.IsNullOrEmpty(groupId), groupId);
        }

        /// <summary>
        /// Checks CUD (Create, Update, Delete) permission (requires 'Product' permission)
        /// </summary>
        private async Task<(bool hasPermission, string groupId)> CheckPermissionForCUDAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                // Requires 'Product' permission for CUD operations
                bool permission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Product);

                if (!permission)
                {
                    return (false, string.Empty);
                }

                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
                groupId = linkedUser?.GroupId ?? string.Empty;
            }
            else
            {
                // Main user has all permissions
                var group = await _userGroupRepository.GetByUserIdAsync(userId);
                groupId = group?.GroupId ?? string.Empty;
            }

            return (!string.IsNullOrEmpty(groupId), groupId);
        }
    }
}