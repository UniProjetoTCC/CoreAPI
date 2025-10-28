using Business.DataRepositories;
using Business.Services.Base;
using Business.Models;
using CoreAPI.Models;
using Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CoreAPI.Controllers
{
    /// <summary>
    /// Controller for monitoring system health and dependencies
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HealthCheckController : ControllerBase
    {
        private readonly CoreAPIContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<HealthCheckController> _logger;
        private readonly ILinkedUserRepository _linkedUserRepository;

        public HealthCheckController(
            CoreAPIContext context,
            UserManager<IdentityUser> userManager,
            ILogger<HealthCheckController> logger,
            ILinkedUserRepository linkedUserRepository)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _linkedUserRepository = linkedUserRepository;
        }

        /// <summary>
        /// Checks if the API is running and responding to requests
        /// </summary>
        /// <remarks>
        /// Simple endpoint that returns OK if the API is up.
        /// Used for monitoring and load balancer health checks.
        /// Does not verify database or other dependencies.
        /// </remarks>
        /// <response code="200">API is up and running</response>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new HealthCheckResponse());
        }

        /// <summary>
        /// Performs a database connection health check
        /// </summary>
        /// <remarks>
        /// Verifies the health of the database connection.
        /// </remarks>
        /// <response code="200">Database connection is healthy</response>
        /// <response code="500">Database connection is unhealthy</response>
        [HttpGet("Database")]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                return Ok(new DatabaseHealthResponse());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Status = "Database Connection Failed",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Checks the current user's information
        /// </summary>
        /// <remarks>
        /// Returns the current user's email, username, and id.
        /// Requires authentication.
        /// </remarks>
        /// <response code="200">User information retrieved successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Error retrieving user information</response>
        [Authorize]
        [HttpGet("User")]
        public async Task<IActionResult> CheckUser()
        {
            try
            {
                var currentUserName = User.Identity?.Name;
                var currentUser = currentUserName != null
                    ? await _userManager.FindByNameAsync(currentUserName)
                    : null;

                if (currentUser == null)
                    return NotFound(new ErrorResponse { Message = "User not found" });

                // Verificar se é um linkeduser
                bool isLinkedUser = await _linkedUserRepository.IsLinkedUserAsync(currentUser.Id);
                LinkedUser? linkedUserDetails = null;

                // Se for um linkeduser, obter detalhes e permissões
                if (isLinkedUser)
                {
                    linkedUserDetails = await _linkedUserRepository.GetByUserIdAsync(currentUser.Id);
                }

                var userRoles = await _userManager.GetRolesAsync(currentUser);

                var response = new HealthCheckUserResponse
                {
                    Status = "Success",
                    User = new UserHealthInfo
                    {
                        Email = currentUser.Email,
                        Username = currentUser.UserName,
                        Id = currentUser.Id,
                        Roles = userRoles,
                        IsLinkedUser = isLinkedUser
                    }
                };

                if (isLinkedUser && linkedUserDetails != null)
                {
                    response.User.LinkedUserDetails = new LinkedUserHealthInfo
                    {
                        Id = linkedUserDetails.Id,
                        ParentUserId = linkedUserDetails.ParentUserId,
                        GroupId = linkedUserDetails.GroupId,
                        IsActive = linkedUserDetails.IsActive,
                        Permissions = new LinkedUserPermissions
                        {
                            CanPerformTransactions = linkedUserDetails.CanPerformTransactions,
                            CanGenerateReports = linkedUserDetails.CanGenerateReports,
                            CanManageProducts = linkedUserDetails.CanManageProducts,
                            CanAlterStock = linkedUserDetails.CanAlterStock,
                            CanManagePromotions = linkedUserDetails.CanManagePromotions
                        }
                    };
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckUser: {Message}", ex.Message);
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
    }
}
