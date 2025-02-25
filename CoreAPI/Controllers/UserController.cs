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
using Microsoft.Extensions.Logging;
using Business.DataRepositories;
using Hangfire;
using Business.Jobs.Background;


namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private const string REFRESH_TOKEN_PURPOSE = "RefreshToken";
        private readonly string PROJECT_NAME;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IRoleService _roleService;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailSenderService _emailSender;
        private readonly ILogger<UserController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILinkedUserRepository _linkedUserRepository;


        public UserController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IRoleService roleService,
            IUserGroupRepository userGroupRepository,
            ISubscriptionPlanRepository subscriptionPlanRepository,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IEmailSenderService emailSender,
            ILogger<UserController> logger,
            ILinkedUserRepository linkedUserRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleService = roleService;
            _userGroupRepository = userGroupRepository;
            _subscriptionPlanRepository = subscriptionPlanRepository;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _logger = logger;
            _linkedUserRepository = linkedUserRepository;

            PROJECT_NAME = configuration["ProjectName"] ??
                throw new InvalidOperationException("ProjectName configuration is not set in appsettings.json!");
        }

        /// <summary>
        /// Registers a new user in the system with a Free subscription plan.
        /// </summary>
        /// <remarks>
        /// This endpoint performs the following steps:
        /// 1. Validates the registration model and checks if user already exists
        /// 2. Creates a new user with the provided credentials
        /// 3. Assigns the FreeUser role and creates a user group with Free plan
        /// 4. Generates and sends email confirmation token
        /// 
        /// The password must meet the following requirements:
        /// - Minimum 8 characters
        /// - At least 1 number
        /// - At least 1 special character
        /// - At least 1 uppercase and 1 lowercase letter
        /// 
        /// On successful registration:
        /// - User is created with FreeUser role
        /// - User group is created with Free subscription plan
        /// - Email confirmation token is generated and sent
        /// - User must confirm email before accessing certain features
        /// 
        /// Error cases:
        /// - User already exists (400 Bad Request)
        /// - Invalid password format (400 Bad Request)
        /// - Error assigning role or creating group (400 Bad Request)
        /// - Error sending confirmation email (200 OK with warning message)
        /// </remarks>
        /// <param name="model">Registration model containing email, username, and password</param>
        /// <response code="200">User created successfully</response>
        /// <response code="400">Invalid data, user already exists, or error during registration</response>
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

            // Create user group with free subscription plan
            try
            {
                // Assign Free plan which will map to FreeUser role
                await _roleService.AssignRoleAsync(user.Id, "FreeUser");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning free plan to user {UserId}", user.Id);
                await _userManager.DeleteAsync(user);
                return BadRequest(new { Message = "Error creating user account. Please try again." });
            }

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
                <p>{PROJECT_NAME} Team</p>";

            try
            {
                await _emailSender.SendEmailAsync(
                    email: user.Email ?? throw new ArgumentNullException(nameof(user.Email)),
                    subject: "Confirm Your Email",
                    message: message
                );
            }
            catch
            {
                await _userManager.DeleteAsync(user);
                return Ok(new { Message = "User created successfully. Try later to confirm your email." });
            }

            return Ok(new { Message = "User created successfully. Please check your email to confirm your account." });
        }

        /// <summary>
        /// Confirms the user's email address
        /// </summary>
        /// <remarks>
        /// Verifies the email confirmation token and updates the user's confirmation status.
        /// </remarks>
        /// <param name="model">Email and confirmation token</param>
        /// <response code="200">Email confirmed successfully</response>
        /// <response code="400">Invalid token</response>
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

        /// <summary>
        /// Resends the email confirmation link
        /// </summary>
        /// <remarks>
        /// Generates a new confirmation token and sends a new email to the user.
        /// </remarks>
        /// <param name="model">User's email</param>
        /// <response code="200">Confirmation email sent successfully</response>
        /// <response code="400">Email not found</response>
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
                <p>{PROJECT_NAME} Team</p>";

            try
            {
                await _emailSender.SendEmailAsync(
                    email: user.Email ?? throw new ArgumentNullException(nameof(user.Email)),
                    subject: "Confirm Your Email",
                    message: message
                );
            }
            catch
            {
                return Ok(new { Message = "Something went wrong. Try later to confirm your email." });
            }

            return Ok(new { Message = "If your email is registered, you will receive a confirmation email." });
        }

        /// <summary>
        /// Authenticates a user with support for two-factor authentication (2FA)
        /// </summary>
        /// <remarks>
        /// If 2FA is enabled, returns requiresTwoFactor=true and the email.
        /// Otherwise, returns access tokens.
        /// Account will be locked after 5 failed attempts for 15 minutes.
        /// </remarks>
        /// <param name="userLogin">User's email and password</param>
        /// <response code="200">Login successful or 2FA required</response>
        /// <response code="401">Invalid credentials or account locked</response>
        /// <response code="400">Invalid data</response>
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel userLogin)
        {
            if (!ModelState.IsValid)
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

        /// <summary>
        /// Handles the forgot password request
        /// </summary>
        /// <param name="forgotPasswordModel">The forgot password model containing the email</param>
        /// <returns>IActionResult indicating success or failure</returns>
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel forgotPasswordModel)
        {
            try
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
                    <p>{PROJECT_NAME} Team</p>";

                try
                {
                    await _emailSender.SendEmailAsync(
                        email: user.Email ?? throw new ArgumentNullException(nameof(user.Email)),
                        subject: "Reset Your Password",
                        message: message
                    );

                    return Ok(new { Message = "If your email is registered, you will receive a password reset token." });
                }
                catch
                {
                    // Don't reveal the error to the user for security
                    return Ok(new { Message = "If your email is registered, you will receive a password reset token." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForgotPassword");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Resets the user's password using a reset token
        /// </summary>
        /// <param name="resetPasswordModel">Model containing email, token and new password</param>
        /// <returns>IActionResult indicating success or failure</returns>
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel resetPasswordModel)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPassword: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Refreshes the authentication token
        /// </summary>
        /// <param name="tokenModel">Model containing the current access and refresh tokens</param>
        /// <returns>IActionResult with new tokens or error</returns>
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenModel tokenModel)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RefreshToken: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Sets up two-factor authentication (2FA) for the user
        /// </summary>
        /// <remarks>
        /// Returns a QR code to be scanned by an authenticator app (Google Authenticator, Microsoft Authenticator, etc.)
        /// and a manual key for backup. 2FA is not active until Enable2FA is called.
        /// </remarks>
        /// <response code="200">QR code and manual key generated successfully</response>
        /// <response code="401">User not authenticated</response>
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
            if (string.IsNullOrEmpty(email))
            {
                throw new InvalidOperationException("User email not found.");
            }

            if (string.IsNullOrEmpty(unformattedKey))
            {
                throw new InvalidOperationException("Authenticator key not found.");
            }

            var authenticatorUri = GenerateQrCodeUri(email, unformattedKey);

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

        /// <summary>
        /// Enables two-factor authentication (2FA) for the user
        /// </summary>
        /// <remarks>
        /// Verifies the code generated by the authenticator app and enables 2FA.
        /// Once enabled, all future logins will require a 2FA code.
        /// </remarks>
        /// <param name="model">Authenticator code</param>
        /// <response code="200">2FA enabled successfully</response>
        /// <response code="400">Invalid code</response>
        /// <response code="401">User not authenticated</response>
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

        /// <summary>
        /// Verifies the 2FA code during login process
        /// </summary>
        /// <remarks>
        /// Used after successful login when 2FA is enabled.
        /// If code is correct, returns access tokens.
        /// Account will be locked after 5 failed attempts for 15 minutes.
        /// </remarks>
        /// <param name="model">Email and authenticator code</param>
        /// <response code="200">Valid code, tokens returned</response>
        /// <response code="400">Invalid code</response>
        /// <response code="401">User not found or account locked</response>
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

        /// <summary>
        /// Changes a user's subscription plan and associated role.
        /// </summary>
        /// <remarks>
        /// This endpoint performs the following steps:
        /// 1. Validates the user exists and has permission to change plans
        /// 2. Verifies the target plan exists and is valid
        /// 3. Checks if user meets requirements for the new plan
        /// 4. Updates user's role and subscription plan
        /// 
        /// Plan upgrade flow:
        /// - Free -> Standard: Requires email verification
        /// - Standard -> Premium: Requires 2FA setup
        /// - Premium -> Enterprise: Requires 2FA and email verification
        /// 
        /// Plan downgrade flow:
        /// - Enterprise -> Premium: No requirements
        /// - Premium -> Standard: No requirements
        /// - Standard -> Free: No requirements
        /// 
        /// Requirements by plan:
        /// - Free: No requirements
        /// - Standard: Email verification
        /// - Premium: Email verification + 2FA
        /// - Enterprise: Email verification + 2FA
        /// 
        /// Error cases:
        /// - User not found (404 Not Found)
        /// - Invalid plan name (400 Bad Request)
        /// - Missing requirements for plan (400 Bad Request)
        /// - Error during plan change (500 Internal Server Error)
        /// </remarks>
        /// <param name="planName">Name of the target subscription plan</param>
        /// <response code="200">Plan changed successfully</response>
        /// <response code="400">Invalid plan or missing requirements</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Error during plan change</response>
        [HttpPost("ChangePlan/{planName}")]
        [Authorize]
        public async Task<IActionResult> ChangePlan(string planName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            string internalPlanName = SetInternalPlanName(planName);

            if (!await _roleManager.RoleExistsAsync(internalPlanName))
            {
                return BadRequest("Role not found");
            }

            // Get user's group before any changes
            var userGroup = await _userGroupRepository.GetByUserIdAsync(user.Id);
            if (userGroup != null)
            {
                try
                {
                    // Obter jobs ativos para o grupo
                    var activeJobs = await _backgroundJobService.GetActiveJobsByGroupId(userGroup.GroupId);
                    if (activeJobs != null)
                    {
                        foreach (var job in activeJobs)
                        {
                            await _backgroundJobService.CancelDowngrade(job.HangfireJobId);
                        }
                    }
                    _logger.LogInformation("Cancelled existing downgrade job for group {GroupId}", userGroup.GroupId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cancel existing downgrade job for group {GroupId}", userGroup.GroupId);
                    // Continue with plan change even if job cancellation fails
                }
            }

            if (await UpgradeDowngradeSubscription(user.Id, planName))
            {
                bool result = await _roleService.AssignRoleAsync(user.Id, internalPlanName);
                if (!result)
                    return BadRequest("Failed to assign role");

                // Schedule downgrade job even on upgrade (for safety)
                if (userGroup != null)
                {
                    try
                    {
                        // Agendar job de downgrade
                        await _backgroundJobService.EnqueueUserDowngrade(userGroup.GroupId);
                        _logger.LogInformation("Scheduled downgrade job for group {GroupId} after plan upgrade", userGroup.GroupId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to schedule downgrade job for user {UserId}", user.Id);
                        // Don't return error since the plan upgrade was successful
                    }
                }

                return Ok("Plan upgraded successfully");
            }
            else
            {
                bool result = await _roleService.AssignRoleAsync(user.Id, internalPlanName);
                if (!result)
                    return BadRequest("Failed to assign role");

                // Schedule downgrade job when plan is downgraded
                if (userGroup != null)
                {
                    try
                    {
                        // Agendar job de downgrade
                        await _backgroundJobService.EnqueueUserDowngrade(userGroup.GroupId);
                        _logger.LogInformation("Scheduled downgrade job for group {GroupId} after plan downgrade", userGroup.GroupId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to schedule downgrade job for user {UserId}", user.Id);
                        // Don't return error since the plan downgrade was successful
                    }
                }

                return Ok("Plan downgraded successfully");
            }
        }

        [HttpPost("CreateLinkedUser")]
        [Authorize]
        public async Task<IActionResult> CreateLinkedUserAsync([FromBody] RegisterLinkedUserModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user found.");
                return Unauthorized();
            }

            var userGroup = await _userGroupRepository.GetByUserIdAsync(currentUser.Id);
            if (userGroup == null) return NotFound("User group not found.");

            var linkedUsers = await _linkedUserRepository.GetAllByGroupIdAsync(userGroup.GroupId);
            if (linkedUsers == null) return NotFound("Linked user not found.");

            var groupPlan = await _subscriptionPlanRepository.GetByIdAsync(userGroup.SubscriptionPlanId);

            if (linkedUsers.Count() >= groupPlan.LinkedUserLimit)
            {
                return BadRequest($"You have reached your limit of linked users!");
            }

            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return BadRequest(new { Message = "User already exists!" });

            IdentityUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };

            var created = await _userManager.CreateAsync(user, model.Password);
            if (!created.Succeeded)
                return BadRequest(new { Errors = created.Errors.Select(e => e.Description) });

            LinkedUserPermissions permissions = new LinkedUserPermissions();
            permissions.CanPerformTransactions = model.CanPerformTransactions;
            permissions.CanGenerateReports = model.CanGenerateReports;
            permissions.CanManageProducts = model.CanManageProducts;
            permissions.CanAlterStock = model.CanAlterStock;
            permissions.CanManagePromotions = model.CanManageProducts;

            var result = await _roleService.AssignLinkedUserAsync(user.Id, currentUser.Id, permissions);
            if (!result)
            {
                return BadRequest("Failed to create linked user.");
            }

            return Ok("Linked user created sucessfully!");
        }

        [HttpPost("UpdateLinkedUser")]
        [Authorize]
        public async Task<IActionResult> UpdateLinkedUserAsync([FromBody] UpdateLinkedUserModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user found");
                return Unauthorized();
            }

            var linkedUser = await _userManager.FindByEmailAsync(model.Email);
            if (linkedUser == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var linkedUserMetadata = await _linkedUserRepository.GetByUserIdAsync(linkedUser.Id);
            if (linkedUserMetadata == null) return NotFound("Linked user not found.");
            if (linkedUserMetadata.ParentUserId != currentUser.Id) return Unauthorized();

            
            var result = await _linkedUserRepository.UpdateLinkedUserAsync(
                linkedUser.Id,
                model.CanPerformTransactions,
                model.CanGenerateReports,
                model.CanManageProducts,
                model.CanAlterStock,
                model.CanManagePromotions
            );
            if (result == null)
            {
                return BadRequest("Failed to update linked user permissions");
            }

            return Ok("Linked user permissions updated succesfully.");
        }

        [HttpPost("DeleteLinkedUser/{email}")]
        [Authorize]
        public async Task<IActionResult> DeleteLinkedUserAsync(string email)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user found.");
                return Unauthorized();
            }

            var linkedUser = await _userManager.FindByEmailAsync(email);
            if(linkedUser == null)
            {
                return NotFound("User not found.");
            }

            //Buscar o linkeduser pelo linkedUserRepository e ver se o usuario que ta fazendo a request(currentUser) é o mesmo que criou o usuario
            var linkedUserMetadata = await _linkedUserRepository.GetByUserIdAsync(linkedUser.Id);
            if (linkedUserMetadata == null) return NotFound("Linked user not found.");
            if (linkedUserMetadata.ParentUserId != currentUser.Id) return Unauthorized();


            var result = await _linkedUserRepository.DeleteLinkedUserAsync(linkedUser.Id);
            if (!result)
            {
                return BadRequest("Failed to delete linked user.");
            }
            return Ok("Linked user deleted successfully!");
        }

        private async Task<bool> UpgradeDowngradeSubscription(string userId, string planName)
        {
            var userGroup = await _userGroupRepository.GetByUserIdAsync(userId);
            if (userGroup == null)
                throw new InvalidOperationException("User group not found");

            var currentPlan = await _subscriptionPlanRepository.GetByIdAsync(userGroup.SubscriptionPlanId);
            if (currentPlan == null)
                throw new InvalidOperationException("Current subscription plan not found");

            var targetPlan = await _subscriptionPlanRepository.GetByNameAsync(planName);
            if (targetPlan == null)
                throw new InvalidOperationException("Target subscription plan not found");

            if (currentPlan.Id == targetPlan.Id)
            {
                throw new InvalidOperationException($"You're already on the {planName} plan");
            }

            // Return true if upgrading (target plan has higher ID), false if downgrading
            return targetPlan.Id > currentPlan.Id;
        }

        private string SetInternalPlanName(string planName)
        {
            Dictionary<string, string> plansDict = new Dictionary<string, string>()
            {
                {"Free", "FreeUser"},
                {"Standard", "StandardPlanUser"},
                {"Premium", "PremiumPlanUser"},
                {"Enterprise", "EnterprisePlanUser"},
                {"Admin", "AdminUser"}
            };

            return plansDict[planName];
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

            return string.Format(
                AuthenticatorUriFormat,
                Uri.EscapeDataString(PROJECT_NAME),
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
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET") ??
                throw new InvalidOperationException("JWT_SECRET environment variable is not set!")));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }

        private string GenerateJwtToken(IdentityUser user)
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