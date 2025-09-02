using AutoMapper;
using Business.DataRepositories;
using Business.Enums;
using Business.Models;
using Business.Services;
using Business.Services.Base;
using CoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAPI.Controllers
{
    /// <summary>
    /// Controller responsible for managing product expirations.
    /// </summary>
    [ApiController]
    [Route("Expiration")]
    public class ProductExpirationController : ControllerBase
    {
        private readonly IProductExpirationRepository _productExpirationRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly ILinkedUserService _linkedUserService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IMapper _mapper;

        public ProductExpirationController(
            IProductExpirationRepository productExpirationRepository,
            IProductRepository productRepository,
            IUserGroupRepository userGroupRepository,
            ILinkedUserRepository linkedUserRepository,
            ILinkedUserService linkedUserService,
            UserManager<IdentityUser> userManager,
            IMapper mapper)
        {
            _productExpirationRepository = productExpirationRepository;
            _productRepository = productRepository;
            _userGroupRepository = userGroupRepository;
            _linkedUserRepository = linkedUserRepository;
            _linkedUserService = linkedUserService;
            _userManager = userManager;
            _mapper = mapper;
        }

        /// <summary>
        /// Get all product expirations for the current group
        /// </summary>
        /// <returns>List of product expirations</returns>
        /// <response code="200">Returns the list of product expirations</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProductExpirationDto>>> GetAll()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var expirations = await _productExpirationRepository.GetAllByGroupIdAsync(groupId);
            var expirationDtos = _mapper.Map<List<ProductExpirationDto>>(expirations);
            return Ok(expirationDtos);
        }

        /// <summary>
        /// Get a product expiration by id
        /// </summary>
        /// <param name="id">Product expiration id</param>
        /// <returns>Product expiration</returns>
        /// <response code="200">Returns the product expiration</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Product expiration not found</response>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ProductExpirationDto>> GetById(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var expiration = await _productExpirationRepository.GetByIdAsync(id, groupId);
            if (expiration == null)
            {
                return NotFound("Product expiration not found or does not belong to your group.");
            }

            var expirationDto = _mapper.Map<ProductExpirationDto>(expiration);
            return Ok(expirationDto);
        }

        /// <summary>
        /// Get product expirations by product id
        /// </summary>
        /// <param name="productId">Product id</param>
        /// <returns>List of product expirations</returns>
        /// <response code="200">Returns the list of product expirations</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        [HttpGet("ByProduct/{productId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProductExpirationDto>>> GetByProductId(string productId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var expirations = await _productExpirationRepository.GetByProductIdAsync(productId, groupId);
            var expirationDtos = _mapper.Map<List<ProductExpirationDto>>(expirations);
            return Ok(expirationDtos);
        }

        /// <summary>
        /// Get product expirations by stock id
        /// </summary>
        /// <param name="stockId">Stock id</param>
        /// <returns>List of product expirations</returns>
        /// <response code="200">Returns the list of product expirations</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        [HttpGet("ByStock/{stockId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProductExpirationDto>>> GetByStockId(string stockId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var expirations = await _productExpirationRepository.GetByStockIdAsync(stockId, groupId);
            var expirationDtos = _mapper.Map<List<ProductExpirationDto>>(expirations);
            return Ok(expirationDtos);
        }

        /// <summary>
        /// Get products expiring in the next X days and already expired products
        /// </summary>
        /// <param name="days">Number of days to look ahead</param>
        /// <returns>List of product expirations</returns>
        /// <response code="200">Returns the list of product expirations</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        [HttpGet("ExpiringInDays/{days:int}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProductExpirationDto>>> GetExpiringInDays(int days)
        {
            if (days < 0)
            {
                return BadRequest("Days must be a positive number");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var expirations = await _productExpirationRepository.GetExpiringInDaysAsync(groupId, days);
            var expirationDtos = _mapper.Map<List<ProductExpirationDto>>(expirations);
            return Ok(expirationDtos);
        }

        /// <summary>
        /// Get expired products
        /// </summary>
        /// <returns>List of expired product expirations</returns>
        /// <response code="200">Returns the list of expired product expirations</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        [HttpGet("Expired")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProductExpirationDto>>> GetExpired()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var expirations = await _productExpirationRepository.GetExpiredAsync(groupId);
            var expirationDtos = _mapper.Map<List<ProductExpirationDto>>(expirations);
            return Ok(expirationDtos);
        }

        /// <summary>
        /// Create a new product expiration
        /// </summary>
        /// <param name="model">Create product expiration model</param>
        /// <returns>Created product expiration</returns>
        /// <response code="201">Returns the created product expiration</response>
        /// <response code="400">Invalid model</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Product or stock not found</response>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ProductExpirationDto>> Create([FromBody] CreateProductExpirationModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate expiration date is in the future
            if (model.ExpirationDate < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("expirationDate", "Expiration date must be in the future");
                return BadRequest(ModelState);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to create product expirations. Only users with Product or Stock permissions can create expirations.");
            }

            var expiration = await _productExpirationRepository.CreateAsync(
                productId: model.ProductId,
                stockId: model.StockId,
                groupId: groupId,
                expirationDate: model.ExpirationDate,
                location: model.Location,
                isActive: model.IsActive
            );

            if (expiration == null)
            {
                return NotFound("Product or stock not found or does not belong to your group.");
            }

            var expirationDto = _mapper.Map<ProductExpirationDto>(expiration);
            return CreatedAtAction(nameof(GetById), new { id = expirationDto.Id }, expirationDto);
        }

        /// <summary>
        /// Update a product expiration
        /// </summary>
        /// <param name="id">Product expiration id</param>
        /// <param name="model">Update product expiration model</param>
        /// <returns>Updated product expiration</returns>
        /// <response code="200">Returns the updated product expiration</response>
        /// <response code="400">Invalid model</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Product expiration not found</response>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ProductExpirationDto>> Update(string id, [FromBody] UpdateProductExpirationModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to update product expirations. Only users with Product or Stock permissions can modify expirations.");
            }

            var expiration = await _productExpirationRepository.UpdateAsync(
                id: id,
                groupId: groupId,
                expirationDate: model.ExpirationDate,
                location: model.Location,
                isActive: model.IsActive
            );

            if (expiration == null)
            {
                return NotFound("Product expiration not found or does not belong to your group.");
            }

            var expirationDto = _mapper.Map<ProductExpirationDto>(expiration);
            return Ok(expirationDto);
        }

        /// <summary>
        /// Delete a product expiration
        /// </summary>
        /// <param name="id">Product expiration id</param>
        /// <returns>No content</returns>
        /// <response code="204">Product expiration deleted</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Product expiration not found</response>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> Delete(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to delete product expirations. Only users with Product or Stock permissions can delete expirations.");
            }

            var expiration = await _productExpirationRepository.DeleteAsync(id, groupId);
            if (expiration == null)
            {
                return NotFound("Product expiration not found or does not belong to your group.");
            }

            return NoContent();
        }

        private async Task<(bool hasPermission, string groupId)> CheckPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                // All linked users can access expiration endpoints (except create/update/delete)
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

        private async Task<(bool hasPermission, string groupId)> CheckPermissionForCUDAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                bool hasProductPermission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Product);
                bool hasStockPermission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Stock);

                if (!hasProductPermission && !hasStockPermission)
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

            return (!string.IsNullOrEmpty(groupId), groupId);
        }
    }
}
