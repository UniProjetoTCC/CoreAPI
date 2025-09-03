using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using CoreAPI.Models;
using Business.Services;
using Business.Services.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreAPI.Controllers
{
    /// <summary>
    /// Controller responsible for managing loyalty programs
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class LoyaltyProgramController : ControllerBase
    {
        private readonly ILoyaltyProgramRepository _loyaltyProgramRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILinkedUserService _linkedUserService;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IMapper _mapper;

        public LoyaltyProgramController(
            ILoyaltyProgramRepository loyaltyProgramRepository,
            ICustomerRepository customerRepository,
            ILinkedUserService linkedUserService,
            IUserGroupRepository userGroupRepository,
            ILinkedUserRepository linkedUserRepository,
            UserManager<IdentityUser> userManager,
            IMapper mapper)
        {
            _loyaltyProgramRepository = loyaltyProgramRepository;
            _customerRepository = customerRepository;
            _linkedUserService = linkedUserService;
            _userGroupRepository = userGroupRepository;
            _linkedUserRepository = linkedUserRepository;
            _userManager = userManager;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets all loyalty programs
        /// </summary>
        /// <remarks>
        /// This endpoint retrieves all loyalty programs for the current user's group.
        /// </remarks>
        /// <response code="200">List of loyalty programs</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<LoyaltyProgramResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(userId);
            if (!hasPermission)
                return Forbid();

            var loyaltyPrograms = await _loyaltyProgramRepository.GetAllAsync(groupId);
            var response = _mapper.Map<List<LoyaltyProgramResponse>>(loyaltyPrograms);

            return Ok(response);
        }

        /// <summary>
        /// Gets a loyalty program by ID
        /// </summary>
        /// <param name="id">Loyalty program ID</param>
        /// <remarks>
        /// This endpoint retrieves a specific loyalty program by its ID.
        /// </remarks>
        /// <response code="200">Loyalty program details</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Loyalty program not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LoyaltyProgramResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(userId);
            if (!hasPermission)
                return Forbid();

            var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(id, groupId);
            if (loyaltyProgram == null)
                return NotFound();

            var response = _mapper.Map<LoyaltyProgramResponse>(loyaltyProgram);

            return Ok(response);
        }

        /// <summary>
        /// Creates a new loyalty program
        /// </summary>
        /// <param name="request">Loyalty program data</param>
        /// <remarks>
        /// This endpoint creates a new loyalty program.
        /// Requires Promotion permission.
        /// </remarks>
        /// <response code="201">Loyalty program successfully created</response>
        /// <response code="400">Invalid data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpPost]
        [ProducesResponseType(typeof(LoyaltyProgramResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Create(LoyaltyProgramRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(userId);
            if (!hasPermission)
                return Forbid("You don't have permission to create loyalty programs. Promotion permission is required.");
            
            // Check if a loyalty program with the same name already exists in this group
            var existingPrograms = await _loyaltyProgramRepository.GetAllAsync(groupId);
            if (existingPrograms.Any(p => p.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest($"A loyalty program with the name '{request.Name}' already exists in this group.");
            }

            var loyaltyProgram = await _loyaltyProgramRepository.CreateAsync(
                name: request.Name,
                description: request.Description,
                centsToPoints: request.CentsToPoints,
                pointsToCents: request.PointsToCents,
                discountPercentage: request.DiscountPercentage,
                groupId: groupId);

            var response = _mapper.Map<LoyaltyProgramResponse>(loyaltyProgram);

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        /// <summary>
        /// Updates an existing loyalty program
        /// </summary>
        /// <param name="id">Loyalty program ID</param>
        /// <param name="request">Updated loyalty program data</param>
        /// <remarks>
        /// This endpoint updates an existing loyalty program.
        /// Requires Promotion permission.
        /// </remarks>
        /// <response code="200">Loyalty program successfully updated</response>
        /// <response code="400">Invalid data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Loyalty program not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(LoyaltyProgramResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(string id, LoyaltyProgramRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(userId);
            if (!hasPermission)
                return Forbid("You don't have permission to update loyalty programs. Promotion permission is required.");

            // Get all loyalty programs for this group
            var existingPrograms = await _loyaltyProgramRepository.GetAllAsync(groupId);
            
            // Find the program being updated
            var existingLoyaltyProgram = existingPrograms.FirstOrDefault(p => p.Id == id);
            if (existingLoyaltyProgram == null)
                return NotFound();
                
            // Check if another program with the same name already exists (excluding the current one)
            if (existingPrograms.Any(p => p.Id != id && p.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest($"Another loyalty program with the name '{request.Name}' already exists in this group.");
            }

            var updatedLoyaltyProgram = await _loyaltyProgramRepository.UpdateAsync(
                id: id,
                groupId: groupId,
                name: request.Name,
                description: request.Description,
                centsToPoints: request.CentsToPoints,
                pointsToCents: request.PointsToCents,
                discountPercentage: request.DiscountPercentage);

            var response = _mapper.Map<LoyaltyProgramResponse>(updatedLoyaltyProgram);

            return Ok(response);
        }

        /// <summary>
        /// Deletes a loyalty program
        /// </summary>
        /// <param name="id">Loyalty program ID</param>
        /// <remarks>
        /// This endpoint deletes a loyalty program.
        /// Requires Promotion permission.
        /// </remarks>
        /// <response code="204">Loyalty program successfully deleted</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Loyalty program not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(userId);
            if (!hasPermission)
                return Forbid("You don't have permission to delete loyalty programs. Promotion permission is required.");

            var existingLoyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(id, groupId);
            if (existingLoyaltyProgram == null)
                return NotFound();

            await _loyaltyProgramRepository.DeleteAsync(id, groupId);

            return NoContent();
        }

        /// <summary>
        /// Activates a loyalty program
        /// </summary>
        /// <param name="id">Loyalty program ID</param>
        /// <remarks>
        /// This endpoint activates a loyalty program.
        /// Requires Promotion permission.
        /// </remarks>
        /// <response code="200">Loyalty program successfully activated</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Loyalty program not found</response>
        [HttpPatch("{id}/Activate")]
        [ProducesResponseType(typeof(LoyaltyProgramResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Activate(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(userId);
            if (!hasPermission)
                return Forbid("You don't have permission to activate loyalty programs. Promotion permission is required.");

            var existingLoyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(id, groupId);
            if (existingLoyaltyProgram == null)
                return NotFound();
                
            // Check if the program is already active
            if (existingLoyaltyProgram.IsActive)
            {
                return BadRequest($"The loyalty program '{existingLoyaltyProgram.Name}' is already active.");
            }

            var activatedLoyaltyProgram = await _loyaltyProgramRepository.ActivateAsync(id, groupId);
            var response = _mapper.Map<LoyaltyProgramResponse>(activatedLoyaltyProgram);

            return Ok(response);
        }

        /// <summary>
        /// Deactivates a loyalty program
        /// </summary>
        /// <param name="id">Loyalty program ID</param>
        /// <remarks>
        /// This endpoint deactivates a loyalty program.
        /// Requires Promotion permission.
        /// </remarks>
        /// <response code="200">Loyalty program successfully deactivated</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Loyalty program not found</response>
        [HttpPatch("{id}/Deactivate")]
        [ProducesResponseType(typeof(LoyaltyProgramResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Deactivate(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(userId);
            if (!hasPermission)
                return Forbid("You don't have permission to deactivate loyalty programs. Promotion permission is required.");

            var existingLoyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(id, groupId);
            if (existingLoyaltyProgram == null)
                return NotFound();
                
            // Check if the program is already inactive
            if (!existingLoyaltyProgram.IsActive)
            {
                return BadRequest($"The loyalty program '{existingLoyaltyProgram.Name}' is already inactive.");
            }
            
            // Check if there are customers with points linked to this loyalty program
            var (hasCustomersWithPoints, customerCount) = await _customerRepository.HasCustomersWithPointsInLoyaltyProgramAsync(id, groupId);
                
            if (hasCustomersWithPoints)
            {
                return BadRequest($"Cannot deactivate loyalty program '{existingLoyaltyProgram.Name}' because there are {customerCount} customers with points linked to it. All customers must have zero points before deactivation.");
            }

            // Deactivate the loyalty program
            var deactivatedLoyaltyProgram = await _loyaltyProgramRepository.DeactivateAsync(id, groupId);
            
            // Unlink all customers from this loyalty program
            int unlinkCount = await _customerRepository.UnlinkAllCustomersFromLoyaltyProgramAsync(id, groupId);
            
            var response = _mapper.Map<LoyaltyProgramResponse>(deactivatedLoyaltyProgram);
            
            return Ok(new {
                Program = response,
                Message = $"Loyalty program '{deactivatedLoyaltyProgram.Name}' has been deactivated and {unlinkCount} customers have been unlinked from it."
            });
        }

        /// <summary>
        /// Gets all active loyalty programs
        /// </summary>
        /// <remarks>
        /// This endpoint retrieves all active loyalty programs for the current user's group.
        /// </remarks>
        /// <response code="200">List of active loyalty programs</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet("Active")]
        [ProducesResponseType(typeof(List<LoyaltyProgramResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetActive()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(userId);
            if (!hasPermission)
                return Forbid();

            var loyaltyPrograms = await _loyaltyProgramRepository.GetActiveAsync(groupId);
            var response = _mapper.Map<List<LoyaltyProgramResponse>>(loyaltyPrograms);

            return Ok(response);
        }

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

        private async Task<(bool hasPermission, string groupId)> CheckPermissionForCUDAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(userId, Business.Enums.LinkedUserPermissionsEnum.Promotion);

                if (!permission)
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
