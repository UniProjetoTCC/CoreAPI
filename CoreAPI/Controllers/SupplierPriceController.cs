using AutoMapper;
using Business.DataRepositories;
using Business.Enums;
using Business.Models;
using Business.Services.Base;
using CoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreAPI.Controllers
{
    /// <summary>
    /// Controller responsible for managing prices offered by suppliers for specific products.
    /// </summary>
    [ApiController]
    [Route("SupplierPrice")]
    [Authorize]
    public class SupplierPriceController : ControllerBase
    {
        private readonly ISupplierPriceRepository _supplierPriceRepository;
        private readonly IProductRepository _productRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<SupplierPriceController> _logger;
        private readonly IMapper _mapper;
        private readonly ILinkedUserService _linkedUserService;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        public SupplierPriceController(
            ISupplierPriceRepository supplierPriceRepository,
            IProductRepository productRepository,
            ISupplierRepository supplierRepository,
            UserManager<IdentityUser> userManager,
            ILogger<SupplierPriceController> logger,
            IMapper mapper,
            ILinkedUserService linkedUserService,
            ILinkedUserRepository linkedUserRepository,
            IUserGroupRepository userGroupRepository)
        {
            _supplierPriceRepository = supplierPriceRepository;
            _productRepository = productRepository;
            _supplierRepository = supplierRepository;
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
            _linkedUserService = linkedUserService;
            _linkedUserRepository = linkedUserRepository;
            _userGroupRepository = userGroupRepository;
        }

        // O endpoint POST (Criar) foi movido para o ProductController
        // para seguir a lógica de "Adicionar Fornecedor a um Produto".

        /// <summary>
        /// Get a specific supplier price by its ID.
        /// </summary>
        /// <param name="id">Supplier Price ID.</param>
        /// <response code="200">Supplier price details.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No permission to access.</response>
        /// <response code="404">Supplier price not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SupplierPriceDto), 200)]
        public async Task<IActionResult> GetSupplierPriceById(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource.");
            }

            var price = await _supplierPriceRepository.GetByIdAsync(id, groupId);
            if (price == null) return NotFound();

            var priceDto = _mapper.Map<SupplierPriceDto>(price);
            return Ok(priceDto);
        }

        /// <summary>
        /// Get all supplier prices for a specific product.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="page">Page number (starts at 1).</param>
        /// <param name="pageSize">Page size.</param>
        /// <response code="200">A paginated list of supplier prices.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No permission to access.</response>
        /// <response code="404">Product not found.</response>
        [HttpGet("Product/{productId}")]
        [ProducesResponseType(typeof(SupplierPriceSearchResponse), 200)]
        public async Task<IActionResult> GetPricesForProduct(string productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource.");
            }

            var product = await _productRepository.GetById(productId, groupId);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            var (items, totalCount) = await _supplierPriceRepository.GetPricesForProductAsync(productId, groupId, page, pageSize);

            var dtos = _mapper.Map<List<SupplierPriceDto>>(items);
            foreach(var dto in dtos)
            {
                dto.ProductName = product.Name; // Set product name since we already have it
            }

            var response = new SupplierPriceSearchResponse
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Pages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(response);
        }

        /// <summary>
        /// Get all product prices from a specific supplier.
        /// </summary>
        /// <param name="supplierId">The ID of the supplier.</param>
        /// <param name="page">Page number (starts at 1).</param>
        /// <param name="pageSize">Page size.</param>
        /// <response code="200">A paginated list of supplier prices.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No permission to access.</response>
        /// <response code="404">Supplier not found.</response>
        [HttpGet("Supplier/{supplierId}")]
        [ProducesResponseType(typeof(SupplierPriceSearchResponse), 200)]
        public async Task<IActionResult> GetPricesFromSupplier(string supplierId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource.");
            }

            var supplier = await _supplierRepository.GetByIdAsync(supplierId, groupId);
            if (supplier == null)
            {
                return NotFound("Supplier not found.");
            }

            var (items, totalCount) = await _supplierPriceRepository.GetPricesFromSupplierAsync(supplierId, groupId, page, pageSize);

            var dtos = _mapper.Map<List<SupplierPriceDto>>(items);
            foreach(var dto in dtos)
            {
                dto.SupplierName = supplier.Name; // Set supplier name since we already have it
            }

            var response = new SupplierPriceSearchResponse
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Pages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(response);
        }

        /// <summary>
        /// Update an existing supplier price.
        /// </summary>
        /// <remarks>
        /// Requires Product permission.
        /// </remarks>
        /// <param name="id">ID of the supplier price to update.</param>
        /// <param name="model">Data to be updated.</param>
        /// <response code="200">Supplier price updated successfully.</response>
        /// <response code="400">Invalid data or duplicate SKU.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No permission to update.</response>
        /// <response code="404">Supplier price not found.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(SupplierPriceDto), 200)]
        public async Task<IActionResult> UpdateSupplierPrice(string id, [FromBody] UpdateSupplierPriceRequest model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to update supplier prices. You need Product permission.");
            }

            try
            {
                var existingPrice = await _supplierPriceRepository.GetByIdAsync(id, groupId);
                if (existingPrice == null) return NotFound();

                // Check for duplicate if SKU changed
                if (existingPrice.SupplierSku.ToLower() != model.SupplierSku.ToLower())
                {
                    bool isDuplicate = await _supplierPriceRepository.CheckDuplicateAsync(existingPrice.ProductId, existingPrice.SupplierId, model.SupplierSku, groupId, id);
                    if (isDuplicate)
                    {
                        return Conflict($"A price entry for this supplier and product with SKU '{model.SupplierSku}' already exists.");
                    }
                }

                var priceBusinessModel = _mapper.Map<SupplierPriceBusinessModel>(model);
                // Preserve required fields not in the update model
                priceBusinessModel.ValidFrom = model.ValidFrom ?? existingPrice.ValidFrom;
                
                var updatedPrice = await _supplierPriceRepository.UpdateAsync(id, groupId, priceBusinessModel);
                if (updatedPrice == null) return NotFound();

                var priceDto = _mapper.Map<SupplierPriceDto>(updatedPrice);
                return Ok(priceDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier price");
                return StatusCode(500, "An error occurred while updating the supplier price.");
            }
        }

        /// <summary>
        /// Activate a supplier price.
        /// </summary>
        /// <remarks>Requires Product permission.</remarks>
        /// <param name="id">ID of the supplier price to activate.</param>
        /// <response code="200">Supplier price activated successfully.</response>
        /// <response code="400">Price is already active.</response>
        /// <response code="403">No permission to modify.</response>
        /// <response code="404">Price not found.</response>
        [HttpPatch("{id}/Activate")]
        [ProducesResponseType(typeof(SupplierPriceDto), 200)]
        public async Task<IActionResult> ActivateSupplierPrice(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to modify supplier prices.");
            }

            var price = await _supplierPriceRepository.GetByIdAsync(id, groupId);
            if (price == null) return NotFound();

            if (price.IsActive)
            {
                return BadRequest("This supplier price is already active.");
            }

            var activatedPrice = await _supplierPriceRepository.ActivateAsync(id, groupId);
            return Ok(_mapper.Map<SupplierPriceDto>(activatedPrice));
        }

        /// <summary>
        /// Deactivate a supplier price.
        /// </summary>
        /// <remarks>Requires Product permission.</remarks>
        /// <param name="id">ID of the supplier price to deactivate.</param>
        /// <response code="200">Supplier price deactivated successfully.</response>
        /// <response code="400">Price is already inactive.</response>
        /// <response code="403">No permission to modify.</response>
        /// <response code="404">Price not found.</response>
        [HttpPatch("{id}/Deactivate")]
        [ProducesResponseType(typeof(SupplierPriceDto), 200)]
        public async Task<IActionResult> DeactivateSupplierPrice(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to modify supplier prices.");
            }

            var price = await _supplierPriceRepository.GetByIdAsync(id, groupId);
            if (price == null) return NotFound();

            if (!price.IsActive)
            {
                return BadRequest("This supplier price is already inactive.");
            }

            var deactivatedPrice = await _supplierPriceRepository.DeactivateAsync(id, groupId);
            return Ok(_mapper.Map<SupplierPriceDto>(deactivatedPrice));
        }
        
        /// <summary>
        /// Deletes a supplier price link.
        /// </summary>
        /// <remarks>Requires Product permission.</remarks>
        /// <param name="id">ID of the supplier price to delete.</param>
        /// <response code="204">Supplier price deleted successfully.</response>
        /// <response code="403">No permission to delete.</response>
        /// <response code="404">Price not found.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteSupplierPrice(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to delete supplier prices.");
            }

            var success = await _supplierPriceRepository.DeleteAsync(id, groupId);
            if (!success)
            {
                return NotFound("Supplier price not found.");
            }

            return NoContent();
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
                // All linked users can view supplier prices
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