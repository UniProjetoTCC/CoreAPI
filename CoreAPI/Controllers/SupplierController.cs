using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using CoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly ISupplierPriceRepository _supplierPriceRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(
            ISupplierRepository supplierRepository,
            ISupplierPriceRepository supplierPriceRepository,
            IProductRepository productRepository,
            IMapper mapper,
            UserManager<IdentityUser> userManager,
            IUserGroupRepository userGroupRepository,
            ILogger<SupplierController> logger)
        {
            _supplierRepository = supplierRepository;
            _supplierPriceRepository = supplierPriceRepository;
            _productRepository = productRepository;
            _mapper = mapper;
            _userManager = userManager;
            _userGroupRepository = userGroupRepository;
            _logger = logger;
        }


        /// <summary>
        /// Creates a new supplier for the current user's group
        /// </summary>
        /// <param name="model">The supplier data</param>
        /// <returns>The created supplier</returns>
        /// <response code="201">Returns the newly created supplier</response>
        /// <response code="409">If a supplier with the same document already exists</response>
        /// <response code="401">If user is not authorized</response>
        [HttpPost]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierCreateModel model)
        {
            var userGroup = await GetUserGroupAsync();
            if (userGroup == null) return Unauthorized();

            var existingSupplier = await _supplierRepository.GetByDocumentAsync(model.Document, userGroup.GroupId);
            if (existingSupplier != null)
            {
                return Conflict("A supplier with this document already exists.");
            }

            var newSupplier = _mapper.Map<SupplierBusinessModel>(model);
            newSupplier.GroupId = userGroup.GroupId;

            var createdSupplier = await _supplierRepository.CreateAsync(newSupplier);
            var dto = _mapper.Map<SupplierDto>(createdSupplier);

            return CreatedAtAction(nameof(GetSupplier), new { id = dto.Id }, dto);
        }

        /// <summary>
        /// Gets a supplier by ID for the current user's group
        /// </summary>
        /// <param name="id">The supplier ID</param>
        /// <returns>The supplier details</returns>
        /// <response code="200">Returns the supplier</response>
        /// <response code="404">If supplier is not found</response>
        /// <response code="401">If user is not authorized</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupplier(string id)
        {
            var userGroup = await GetUserGroupAsync();
            if (userGroup == null) return Unauthorized();

            var supplier = await _supplierRepository.GetByIdAsync(id, userGroup.GroupId);
            if (supplier == null) return NotFound();

            return Ok(_mapper.Map<SupplierDto>(supplier));
        }

        /// <summary>
        /// Searches suppliers by name with pagination
        /// </summary>
        /// <param name="name">Optional name to search for</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 20)</param>
        /// <returns>List of suppliers matching the search criteria</returns>
        /// <response code="200">Returns the list of suppliers</response>
        /// <response code="401">If user is not authorized</response>
        [HttpGet("Search")]
        public async Task<IActionResult> SearchSuppliers([FromQuery] string? name, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userGroup = await GetUserGroupAsync();
            if (userGroup == null) return Unauthorized();

            var (items, totalCount) = await _supplierRepository.SearchAsync(userGroup.GroupId, name, page, pageSize);

            return Ok(new SupplierSearchResponse
            {
                Items = _mapper.Map<List<SupplierDto>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Pages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        /// <summary>
        /// Updates an existing supplier
        /// </summary>
        /// <param name="model">The supplier update data</param>
        /// <returns>The updated supplier</returns>
        /// <response code="200">Returns the updated supplier</response>
        /// <response code="404">If supplier is not found</response>
        /// <response code="409">If another supplier with the same document exists</response>
        /// <response code="401">If user is not authorized</response>
        [HttpPut]
        public async Task<IActionResult> UpdateSupplier([FromBody] SupplierUpdateModel model)
        {
            var userGroup = await GetUserGroupAsync();
            if (userGroup == null) return Unauthorized();

            var existingSupplier = await _supplierRepository.GetByIdAsync(model.Id, userGroup.GroupId);
            if (existingSupplier == null) return NotFound();

            if (existingSupplier.Document != model.Document)
            {
                var duplicateSupplier = await _supplierRepository.GetByDocumentAsync(model.Document, userGroup.GroupId);
                if (duplicateSupplier != null)
                {
                    return Conflict("Another supplier with this document already exists.");
                }
            }

            var supplierToUpdate = _mapper.Map<SupplierBusinessModel>(model);
            supplierToUpdate.GroupId = userGroup.GroupId;

            var updatedSupplier = await _supplierRepository.UpdateAsync(supplierToUpdate);
            return Ok(_mapper.Map<SupplierDto>(updatedSupplier));
        }

        /// <summary>
        /// Deletes a supplier by ID
        /// </summary>
        /// <param name="id">The supplier ID to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If supplier was successfully deleted</response>
        /// <response code="404">If supplier is not found</response>
        /// <response code="401">If user is not authorized</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(string id)
        {
            var userGroup = await GetUserGroupAsync();
            if (userGroup == null) return Unauthorized();

            var success = await _supplierRepository.DeleteAsync(id, userGroup.GroupId);
            if (!success) return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Gets all product prices for a supplier
        /// </summary>
        /// <param name="supplierId">The supplier ID</param>
        /// <returns>List of prices for the supplier</returns>
        /// <response code="200">Returns the list of prices</response>
        /// <response code="401">If user is not authorized</response>
        [HttpGet("{supplierId}/prices")]
        public async Task<IActionResult> GetSupplierPrices(string supplierId)
        {
            var userGroup = await GetUserGroupAsync();
            if (userGroup == null) return Unauthorized();

            var prices = await _supplierPriceRepository.GetBySupplierIdAsync(supplierId, userGroup.GroupId);
            return Ok(_mapper.Map<List<SupplierPriceDto>>(prices));
        }

        /// <summary>
        /// Updates a product price
        /// </summary>
        /// <param name="model">The price update data</param>
        /// <returns>The updated price details</returns>
        /// <response code="200">Returns the updated price</response>
        /// <response code="404">If price is not found</response>
        /// <response code="401">If user is not authorized</response>
        [HttpPut("prices")]
        public async Task<IActionResult> UpdateProductPrice(string supplierId, [FromBody] SupplierPriceUpdateModel model)
        {
            var userGroup = await GetUserGroupAsync();
            if (userGroup == null) return Unauthorized();

            // Busca o preço existente pelo seu ID (que vem no corpo)
            var existingPrice = await _supplierPriceRepository.GetByIdAsync(model.Id, userGroup.GroupId);
            if (existingPrice == null) return NotFound();

            // 🔹 Atualiza os campos normais (price, etc.) usando o AutoMapper
            var priceToUpdate = _mapper.Map(model, existingPrice);

            var updatedPrice = await _supplierPriceRepository.UpdateAsync(priceToUpdate);

            return Ok(_mapper.Map<SupplierPriceDto>(updatedPrice));
        }

        private async Task<UserGroup?> GetUserGroupAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return null;
            return await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
        }
    }
}