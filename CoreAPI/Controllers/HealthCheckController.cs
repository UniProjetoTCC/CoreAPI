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

        public HealthCheckController(
            CoreAPIContext context,
            UserManager<IdentityUser> userManager,
            ILogger<HealthCheckController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
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
            return Ok(new { Status = "healthy", Timestamp = DateTime.UtcNow });
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
                return Ok(new
                {
                    Status = "Database Connection Successful",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = "Database Connection Failed",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
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
                    return NotFound(new { Status = "Error", Message = "User not found" });

                return Ok(new
                {
                    Status = "Success",
                    User = new
                    {
                        Email = currentUser.Email,
                        Username = currentUser.UserName,
                        Id = currentUser.Id
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
        }
    }
}
