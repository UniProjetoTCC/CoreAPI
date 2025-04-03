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
        private readonly IMapper _mapper;

        public ProductController(
            ILinkedUserService linkedUserService, 
            IProductRepository productRepository, 
            UserManager<IdentityUser> userManager, 
            IUserGroupRepository userGroupRepository,
            IMapper mapper)
        {
            _linkedUserService = linkedUserService;
            _productRepository = productRepository;
            _userManager = userManager;
            _userGroupRepository = userGroupRepository;
            _mapper = mapper;
        }

        [HttpGet("Get")]
        [Authorize]
        public async Task<ActionResult> Get([FromBody] GetByID model)
        {
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

            var product = await _productRepository.GetById(model.Id, groupId);

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
            
            // Map from the API model to the business model
            var productBusinessModel = _mapper.Map<ProductBusinessModel>(model);
            
            // Set the group ID from the current user's group
            productBusinessModel.GroupId = groupId;

            var product = await _productRepository.CreateProductAsync(productBusinessModel);

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
            
            // Map from the API model to the business model
            var productBusinessModel = _mapper.Map<ProductBusinessModel>(model);
            
            // Set the group ID from the current user's group
            productBusinessModel.GroupId = groupId;

            var product = await _productRepository.UpdateProductAsync(productBusinessModel);

            return product != null ? Ok(product) : NotFound();
        }
    }
}