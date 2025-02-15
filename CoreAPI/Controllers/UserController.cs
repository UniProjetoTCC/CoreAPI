using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CoreAPI.Models;
using Microsoft.Extensions.Configuration;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;

        public UserController(
            UserManager<IdentityUser> userManager, 
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserModel userRegister)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Status = "Error",
                    Message = "Invalid data",
                    Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                });
            }

            IdentityUser user = new IdentityUser
            {
                UserName = userRegister.Username,
                Email = userRegister.Email
            };

            var result = await _userManager.CreateAsync(user, userRegister.Password);

            if(result.Succeeded)
            {
                return Ok(new 
                {
                    Status = "Success",
                    Message = "User created successfully"
                });
            }
            else
            {
                return BadRequest(new 
                {
                    Status = "Error",
                    Message = "User not created",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel userLogin)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(new 
                {
                    Status = "Error",
                    Message = "Invalid data",
                    Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                });
            }

            IdentityUser user = await _userManager.FindByEmailAsync(userLogin.Email);

            if (user is { UserName: not null, Email: not null, Id: not null })
            {
                var result = await _signInManager.PasswordSignInAsync(user, userLogin.Password, false, false);

                if (result.Succeeded)
                {
                    var userClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    };

                    var token = GetToken(userClaims);

                    return Ok(new 
                    {
                        Status = "Success",
                        Token = new JwtSecurityTokenHandler().WriteToken(token),
                        Expiration = token.ValidTo
                    });
                }

                return Unauthorized(new 
                {
                    Status = "Error",
                    Message = "Invalid credentials"
                });
            }
            else
            {
                return BadRequest(new 
                {
                    Status = "Error",
                    Message = "User not found",
                });
            }
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? 
                throw new InvalidOperationException("JWT_SECRET environment variable is not set!");

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }
    }
}