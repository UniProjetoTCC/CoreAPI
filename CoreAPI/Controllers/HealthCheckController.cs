using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data.Context;

namespace CoreAPI.Controllers
{
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

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Status = "healthy", Timestamp = DateTime.UtcNow });
        }

        [HttpGet("Database")]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                return Ok(new { 
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

                return Ok(new { 
                    Status = "Success",
                    User = new {
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
