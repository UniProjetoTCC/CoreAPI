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
        private readonly IMapper _mapper;

        public ProductController(
            ILinkedUserService linkedUserService, 
            IProductRepository productRepository, 
            UserManager<IdentityUser> userManager, 
            IUserGroupRepository userGroupRepository,
            ICategoryRepository categoryRepository,
            IMapper mapper)
        {
            _linkedUserService = linkedUserService;
            _productRepository = productRepository;
            _userManager = userManager;
            _userGroupRepository = userGroupRepository;
            _categoryRepository = categoryRepository;
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
            
            var group = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
            string groupId = group?.GroupId ?? string.Empty;

            if (await _linkedUserService.IsLinkedUserAsync(currentUser.Id))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(currentUser.Id, LinkedUserPermissionsEnum.Product);

                if (!permission)
                {
                    return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
                }
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
            
            var group = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
            string groupId = group?.GroupId ?? string.Empty;

            if (await _linkedUserService.IsLinkedUserAsync(currentUser.Id))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(currentUser.Id, LinkedUserPermissionsEnum.Product);

                if (!permission)
                {
                    return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
                }
            }
            
            // Find category by name or create a new one
            var category = await _categoryRepository.GetByNameAsync(model.CategoryName, groupId);
            string categoryId;
            
            if (category == null)
            {
                // Create a new category and get the ID directly
                categoryId = await _categoryRepository.CreateCategoryAsync(
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

            return product != null ? Ok(product) : BadRequest();
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
            
            var group = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
            string groupId = group?.GroupId ?? string.Empty;

            if (await _linkedUserService.IsLinkedUserAsync(currentUser.Id))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(currentUser.Id, LinkedUserPermissionsEnum.Product);

                if (!permission)
                {
                    return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
                }
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
    }
}