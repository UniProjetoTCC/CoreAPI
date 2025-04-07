using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data.Context;
using CoreAPI.Models;
using Business.Models;
using Business.Services.Base;
using Business.DataRepositories;
using Business.Enums;
using AutoMapper;

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

        public ProductController(
            ILinkedUserService linkedUserService, 
            IProductRepository productRepository, 
            UserManager<IdentityUser> userManager, 
            IUserGroupRepository userGroupRepository,
            ICategoryRepository categoryRepository,
            ILinkedUserRepository linkedUserRepository,
            IMapper mapper)
        {
            _linkedUserService = linkedUserService;
            _productRepository = productRepository;
            _userManager = userManager;
            _userGroupRepository = userGroupRepository;
            _categoryRepository = categoryRepository;
            _linkedUserRepository = linkedUserRepository;
            _mapper = mapper;
        }

        [HttpGet("Get")]
        [Authorize]
        public async Task<ActionResult> Get([FromQuery] string id)
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
            string categoryId;
            
            if (category == null)
            {
                // Create a new category and get the ID directly
                categoryId = await _categoryRepository.CreateCategoryGetIdAsync(
                    name: model.CategoryName,
                    groupId: groupId,
                    description: $"Category automatically created for product {model.Name}",
                    active: true
                );
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