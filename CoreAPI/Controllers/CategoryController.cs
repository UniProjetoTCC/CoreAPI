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
    public class CategoryController : ControllerBase
    {
        private readonly ILinkedUserService _linkedUserService;
        private readonly IProductRepository _productRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly ILogger<CategoryController> _logger;
        private readonly IMapper _mapper;

        public CategoryController(
        ILinkedUserService linkedUserService,
        IProductRepository productRepository,
        UserManager<IdentityUser> userManager,
        IUserGroupRepository userGroupRepository,
        ICategoryRepository categoryRepository,
        ILinkedUserRepository linkedUserRepository,
        ILogger<CategoryController> logger,
        IMapper mapper)
        {
            _linkedUserService = linkedUserService;
            _productRepository = productRepository;
            _userManager = userManager;
            _userGroupRepository = userGroupRepository;
            _categoryRepository = categoryRepository;
            _linkedUserRepository = linkedUserRepository;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet("Get")]
        [Authorize]
        public async Task<ActionResult> Get([FromQuery] string id)
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

            return Ok(category);
        }

        [HttpPost("Create")]
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
                    return Forbid("You do not have permission to access this resource.");
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

            return CreatedAtAction(nameof(Get), new { id = newCategory.Id }, newCategory);
        }

        [HttpPost("Update")]
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
            return Ok(category);

        }
    }
}