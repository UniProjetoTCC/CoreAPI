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
using System.Net;
using System.Security.Claims;

namespace CoreAPI.Controllers
{
    /// <summary>
    /// Controller responsible for managing payment methods.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("PaymentMethods")]
    public class PaymentMethodController : ControllerBase
    {
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILinkedUserService _linkedUserService;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        public PaymentMethodController(
            IPaymentMethodRepository paymentMethodRepository,
            UserManager<IdentityUser> userManager,
            IMapper mapper,
            ILinkedUserService linkedUserService,
            ILinkedUserRepository linkedUserRepository,
            IUserGroupRepository userGroupRepository)
        {
            _paymentMethodRepository = paymentMethodRepository;
            _userManager = userManager;
            _mapper = mapper;
            _linkedUserService = linkedUserService;
            _linkedUserRepository = linkedUserRepository;
            _userGroupRepository = userGroupRepository;
        }

        /// <summary>
        /// Gets all payment methods for the user's group
        /// </summary>
        /// <remarks>
        /// This endpoint returns all payment methods associated with the authenticated user's group,
        /// regardless of whether they are active or not.
        /// </remarks>
        /// <response code="200">List of payment methods</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<PaymentMethodResponse>), (int)HttpStatusCode.OK)]
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

            var paymentMethods = await _paymentMethodRepository.GetAllAsync(groupId);
            var response = _mapper.Map<List<PaymentMethodResponse>>(paymentMethods);

            return Ok(response);
        }

        /// <summary>
        /// Gets all active payment methods for the user's group
        /// </summary>
        /// <remarks>
        /// This endpoint returns only the active payment methods associated with the authenticated user's group.
        /// </remarks>
        /// <response code="200">List of active payment methods</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpGet("Active")]
        [ProducesResponseType(typeof(List<PaymentMethodResponse>), (int)HttpStatusCode.OK)]
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

            var paymentMethods = await _paymentMethodRepository.GetActiveAsync(groupId);
            var response = _mapper.Map<List<PaymentMethodResponse>>(paymentMethods);

            return Ok(response);
        }

        /// <summary>
        /// Gets a specific payment method by ID
        /// </summary>
        /// <param name="id">Payment method ID</param>
        /// <remarks>
        /// This endpoint returns a specific payment method by its ID.
        /// The payment method must belong to the authenticated user's group.
        /// </remarks>
        /// <response code="200">Payment method found</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Payment method not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PaymentMethodResponse), (int)HttpStatusCode.OK)]
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

            var paymentMethod = await _paymentMethodRepository.GetByIdAsync(id, groupId);

            if (paymentMethod == null)
                return NotFound();

            var response = _mapper.Map<PaymentMethodResponse>(paymentMethod);

            return Ok(response);
        }

        /// <summary>
        /// Creates a new payment method
        /// </summary>
        /// <param name="request">Payment method data</param>
        /// <remarks>
        /// This endpoint creates a new payment method associated with the authenticated user's group.
        /// Requires Transaction or Product permission.
        /// </remarks>
        /// <response code="201">Payment method successfully created</response>
        /// <response code="400">Invalid data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        [HttpPost]
        [ProducesResponseType(typeof(PaymentMethodResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Create(PaymentMethodRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(userId);
            if (!hasPermission)
                return Forbid("You don't have permission to create payment methods. Transaction or Product permission is required.");

            // Check if a payment method with the same code already exists for this group
            var existingPaymentMethods = await _paymentMethodRepository.GetAllAsync(groupId);
            if (existingPaymentMethods.Any(p => p.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest($"A payment method with code '{request.Code}' already exists for this group. Please use a unique code.");
            }

            var paymentMethod = await _paymentMethodRepository.CreateAsync(
                name: request.Name,
                code: request.Code,
                description: request.Description,
                groupId: groupId);

            var response = _mapper.Map<PaymentMethodResponse>(paymentMethod);

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        /// <summary>
        /// Updates an existing payment method
        /// </summary>
        /// <param name="id">Payment method ID</param>
        /// <param name="request">Updated payment method data</param>
        /// <remarks>
        /// This endpoint updates an existing payment method.
        /// Requires Transaction or Product permission.
        /// </remarks>
        /// <response code="200">Payment method successfully updated</response>
        /// <response code="400">Invalid data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Payment method not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(PaymentMethodResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(string id, PaymentMethodRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionForCUDAsync(userId);
            if (!hasPermission)
                return Forbid("You don't have permission to update payment methods. Transaction or Product permission is required.");

            var existingPaymentMethod = await _paymentMethodRepository.GetByIdAsync(id, groupId);
            if (existingPaymentMethod == null)
                return NotFound();

            // Check if another payment method with the same code already exists for this group
            var existingPaymentMethods = await _paymentMethodRepository.GetAllAsync(groupId);
            if (existingPaymentMethods.Any(p => p.Id != id && p.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest($"Another payment method with code '{request.Code}' already exists for this group. Please use a unique code.");
            }

            var updatedPaymentMethod = await _paymentMethodRepository.UpdateAsync(
                id: id,
                groupId: groupId,
                name: request.Name,
                code: request.Code,
                description: request.Description);

            var response = _mapper.Map<PaymentMethodResponse>(updatedPaymentMethod);

            return Ok(response);
        }

        /// <summary>
        /// Deletes a payment method
        /// </summary>
        /// <param name="id">Payment method ID</param>
        /// <remarks>
        /// This endpoint deletes a payment method.
        /// Requires Transaction or Product permission.
        /// </remarks>
        /// <response code="204">Payment method successfully deleted</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Payment method not found</response>
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
                return Forbid("You don't have permission to delete payment methods. Transaction or Product permission is required.");

            var existingPaymentMethod = await _paymentMethodRepository.GetByIdAsync(id, groupId);
            if (existingPaymentMethod == null)
                return NotFound();

            await _paymentMethodRepository.DeleteAsync(id, groupId);

            return NoContent();
        }

        /// <summary>
        /// Activates a payment method
        /// </summary>
        /// <param name="id">Payment method ID</param>
        /// <remarks>
        /// This endpoint activates an existing payment method.
        /// Requires Transaction or Product permission.
        /// </remarks>
        /// <response code="200">Payment method successfully activated</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Payment method not found</response>
        [HttpPatch("{id}/Activate")]
        [ProducesResponseType(typeof(PaymentMethodResponse), (int)HttpStatusCode.OK)]
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
                return Forbid("You don't have permission to activate payment methods. Transaction or Product permission is required.");

            var existingPaymentMethod = await _paymentMethodRepository.GetByIdAsync(id, groupId);
            if (existingPaymentMethod == null)
                return NotFound();

            var activatedPaymentMethod = await _paymentMethodRepository.ActivateAsync(id, groupId);
            var response = _mapper.Map<PaymentMethodResponse>(activatedPaymentMethod);

            return Ok(response);
        }

        /// <summary>
        /// Deactivates a payment method
        /// </summary>
        /// <param name="id">Payment method ID</param>
        /// <remarks>
        /// This endpoint deactivates an existing payment method.
        /// Requires Transaction or Product permission.
        /// </remarks>
        /// <response code="200">Payment method successfully deactivated</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Insufficient permissions</response>
        /// <response code="404">Payment method not found</response>
        [HttpPatch("{id}/Deactivate")]
        [ProducesResponseType(typeof(PaymentMethodResponse), (int)HttpStatusCode.OK)]
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
                return Forbid("You don't have permission to deactivate payment methods. Transaction or Product permission is required.");

            var existingPaymentMethod = await _paymentMethodRepository.GetByIdAsync(id, groupId);
            if (existingPaymentMethod == null)
                return NotFound();

            var deactivatedPaymentMethod = await _paymentMethodRepository.DeactivateAsync(id, groupId);
            var response = _mapper.Map<PaymentMethodResponse>(deactivatedPaymentMethod);
            
            return Ok(response);
        }

        private async Task<(bool hasPermission, string groupId)> CheckPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                // All linked users can access payment method endpoints (read-only)
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
                bool hasTransactionPermission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Transaction);
                bool hasProductPermission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Product);

                if (!hasTransactionPermission && !hasProductPermission)
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
