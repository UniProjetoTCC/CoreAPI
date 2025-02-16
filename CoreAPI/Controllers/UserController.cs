using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoreAPI.Models;
using Microsoft.Extensions.Configuration;
using Business.Services.Base;
using QRCoder;
using System.Drawing;
using System.IO;
using System;
using Microsoft.AspNetCore.Authorization;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private const string REFRESH_TOKEN_PURPOSE = "RefreshToken";
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSenderService _emailSender;

        public UserController(
            UserManager<IdentityUser> userManager, 
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IEmailSenderService emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return BadRequest(new { Message = "User already exists!" });

            IdentityUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

            // Generate email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var message = $@"
                <h2>Confirm Your Email</h2>
                <p>Hello {user.UserName},</p>
                <p>Thank you for registering. Please use the token below to confirm your email address:</p>
                <p style='background-color: #f5f5f5; padding: 10px; font-family: monospace;'>{token}</p>
                <p>If you didn't create an account, you can safely ignore this email.</p>
                <br>
                <p>Best regards,</p>
                <p>UniProjeto TCC Team</p>";

            try
            {
                await _emailSender.SendEmailAsync(
                    email: user.Email,
                    subject: "Confirm Your Email",
                    message: message
                );
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);
                return Ok(new { Message = "User created successfully. Try later to confirm your email." });
            }

            return Ok(new { Message = "User created successfully. Please check your email to confirm your account." });
        }

        [HttpPost("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest("Invalid request");

            var result = await _userManager.ConfirmEmailAsync(user, model.Token);
            if (result.Succeeded)
                return Ok(new { Message = "Email confirmed successfully." });

            return BadRequest(new
            {
                Message = "Error confirming email",
                Errors = result.Errors.Select(e => e.Description)
            });
        }

        [HttpPost("ResendEmailConfirmation")]
        public async Task<IActionResult> ResendEmailConfirmation([FromBody] EmailModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Ok(new { Message = "If your email is registered, you will receive a confirmation email." });

            if (user.EmailConfirmed)
                return BadRequest(new { Message = "Email is already confirmed." });

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var message = $@"
                <h2>Confirm Your Email</h2>
                <p>Hello {user.UserName},</p>
                <p>Please use the token below to confirm your email address:</p>
                <p style='background-color: #f5f5f5; padding: 10px; font-family: monospace;'>{token}</p>
                <p>If you didn't request this confirmation, you can safely ignore this email.</p>
                <br>
                <p>Best regards,</p>
                <p>UniProjeto TCC Team</p>";

            try
            {
                await _emailSender.SendEmailAsync(
                    email: user.Email,
                    subject: "Confirm Your Email",
                    message: message
                );
            }
            catch (Exception ex)
            {
                return Ok(new { Message = "Something went wrong. Try later to confirm your email." });
            }

            return Ok(new { Message = "If your email is registered, you will receive a confirmation email." });
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

            var user = await _userManager.FindByEmailAsync(userLogin.Email);
            if (user == null)
            {
                return Unauthorized(new { Status = "Error", Message = "Invalid email or password" });
            }

            var result = await _signInManager.PasswordSignInAsync(user, userLogin.Password, false, true);

            if (result.IsLockedOut)
            {
                return Unauthorized(new { Status = "Error", Message = "Account is locked out" });
            }

            if (!result.Succeeded)
            {
                if (result.RequiresTwoFactor)
                {
                    return Ok(new TokenModel
                    {
                        RequiresTwoFactor = true,
                        Email = user.Email
                    });
                }

                return Unauthorized(new { Status = "Error", Message = "Invalid email or password" });
            }

            var userClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var jwtToken = GetToken(userClaims);
            var refreshToken = await GenerateRefreshTokenAsync(user);

            return Ok(new TokenModel
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                RefreshToken = refreshToken,
                RequiresTwoFactor = false
            });
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel forgotPasswordModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(forgotPasswordModel.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return Ok(new { Message = "If your email is registered, you will receive a password reset token." });
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var message = $@"
                <h2>Reset Your Password</h2>
                <p>Hello,</p>
                <p>We received a request to reset your password. Use the token below with the reset password endpoint:</p>
                <p style='background-color: #f5f5f5; padding: 10px; font-family: monospace;'>{token}</p>
                <p>If you didn't request this, you can safely ignore this email.</p>
                <p>The token will expire in 24 hours.</p>
                <br>
                <p>Best regards,</p>
                <p>UniProjeto TCC Team</p>";

            try
            {
                await _emailSender.SendEmailAsync(
                    email: user.Email,
                    subject: "Reset Your Password",
                    message: message
                );

                return Ok(new { Message = "If your email is registered, you will receive a password reset token." });
            }
            catch (Exception ex)
            {
                // Don't reveal the error to the user for security
                return Ok(new { Message = "If your email is registered, you will receive a password reset token." });
            }
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel resetPasswordModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(resetPasswordModel.Email);
            if (user == null)
            {
                return BadRequest("Invalid request");
            }

            var result = await _userManager.ResetPasswordAsync(user, 
                resetPasswordModel.Token, 
                resetPasswordModel.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Password has been reset successfully." });
            }

            return BadRequest(new
            {
                Message = "Error resetting password",
                Errors = result.Errors.Select(e => e.Description)
            });
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenModel tokenModel)
        {
            if (tokenModel is null || string.IsNullOrEmpty(tokenModel.RefreshToken))
            {
                return BadRequest("Invalid client request");
            }

            // Extract the user claims from the token
            var principal = GetPrincipalFromExpiredToken(tokenModel.AccessToken);
            if (principal?.Identity?.Name == null)
            {
                return BadRequest("Invalid access token");
            }

            var user = await _userManager.FindByNameAsync(principal.Identity.Name);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            // Validate the refresh token
            var isValid = await _userManager.VerifyUserTokenAsync(
                user,
                TokenOptions.DefaultProvider,
                REFRESH_TOKEN_PURPOSE,
                tokenModel.RefreshToken);

            if (!isValid)
            {
                return BadRequest("Invalid refresh token");
            }

            // Remove the old refresh token
            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                TokenOptions.DefaultProvider,
                REFRESH_TOKEN_PURPOSE);

            // Generate new token and refresh token
            var newAccessToken = GetToken(principal.Claims.ToList());
            var newRefreshToken = await GenerateRefreshTokenAsync(user);

            return Ok(new TokenModel
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                RefreshToken = newRefreshToken
            });
        }

        [HttpGet("Setup2FA")]
        [Authorize]
        public async Task<IActionResult> Setup2FA()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Generate the 2FA key
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var email = await _userManager.GetEmailAsync(user);
            var authenticatorUri = GenerateQrCodeUri(email!, unformattedKey!);

            // Generate QR code
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = Convert.ToBase64String(qrCode.GetGraphic(20));

            return Ok(new TwoFactorResponse
            {
                QrCodeUrl = $"data:image/png;base64,{qrCodeImage}",
                ManualEntryKey = unformattedKey
            });
        }

        [HttpPost("Enable2FA")]
        [Authorize]
        public async Task<IActionResult> Enable2FA([FromBody] Enable2faModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, model.Code);

            if (!isValid)
            {
                return BadRequest(new { Message = "Invalid verification code" });
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            return Ok(new { Message = "2FA has been enabled successfully" });
        }

        [HttpPost("Verify2FA")]
        public async Task<IActionResult> Verify2FA([FromBody] Verify2faModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, model.Code);

            if (!isValid)
            {
                return BadRequest(new { Message = "Invalid verification code" });
            }

            var userClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var jwtToken = GetToken(userClaims);
            var refreshToken = await GenerateRefreshTokenAsync(user);

            return Ok(new TokenModel
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                RefreshToken = refreshToken,
                RequiresTwoFactor = false
            });
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
            
            return string.Format(
                AuthenticatorUriFormat,
                Uri.EscapeDataString("UniProjetoTCC"),
                Uri.EscapeDataString(email),
                unformattedKey);
        }

        private async Task<string> GenerateRefreshTokenAsync(IdentityUser user)
        {
            var refreshToken = await _userManager.GenerateUserTokenAsync(
                user,
                TokenOptions.DefaultProvider,
                REFRESH_TOKEN_PURPOSE);

            await _userManager.SetAuthenticationTokenAsync(
                user,
                TokenOptions.DefaultProvider,
                REFRESH_TOKEN_PURPOSE,
                refreshToken);

            return refreshToken;
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? 
                throw new InvalidOperationException("JWT_SECRET environment variable is not set!");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateLifetime = false, // Não valida expiração
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                ValidAudience = _configuration["JWT:ValidAudience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, 
                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
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

        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? 
                throw new InvalidOperationException("JWT_SECRET environment variable is not set!");

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new(ClaimTypes.Email, user.Email ?? string.Empty)
                },
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}