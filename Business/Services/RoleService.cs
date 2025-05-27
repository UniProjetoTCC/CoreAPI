using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Business.DataRepositories;
using Business.Models;
using Business.Services.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Business.Services
{
    /// <summary>
    /// Service for managing roles and subscription plans.
    /// </summary>
    public class RoleService : IRoleService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RoleService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleService"/> class.
        /// </summary>
        /// <param name="userManager">User manager instance.</param>
        /// <param name="roleManager">Role manager instance.</param>
        /// <param name="userGroupRepository">User group repository instance.</param>
        /// <param name="subscriptionPlanRepository">Subscription plan repository instance.</param>
        /// <param name="linkedUserRepository">Linked user repository instance.</param>
        /// <param name="configuration">Application configuration instance.</param>
        /// <param name="logger">Logger instance.</param>
        public RoleService(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserGroupRepository userGroupRepository,
            ISubscriptionPlanRepository subscriptionPlanRepository,
            ILinkedUserRepository linkedUserRepository,
            IConfiguration configuration,
            ILogger<RoleService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userGroupRepository = userGroupRepository;
            _subscriptionPlanRepository = subscriptionPlanRepository;
            _linkedUserRepository = linkedUserRepository;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Sets up initial roles and subscription plans in the system.
        /// This is called during application startup to ensure all required roles exist.
        /// </summary>
        public async Task SetupRolesAndPlansAsync()
        {
            _logger.LogInformation("Starting roles and plans setup");

            // Setup Roles
            var roles = new[]
            {
                "FreeUser",
                "StandardPlanUser",
                "PremiumPlanUser",
                "EnterprisePlanUser",
                "AdminUser",
                "LinkedUser"
            };

            foreach (var roleName in roles)
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    _logger.LogInformation("Creating role: {RoleName}", roleName);
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Setup Subscription Plans
            var plansConfig = _configuration.GetSection("Plans");

            await CreatePlanIfNotExistsAsync("Free", plansConfig.GetSection("Free"));
            await CreatePlanIfNotExistsAsync("Standard", plansConfig.GetSection("Standard"));
            await CreatePlanIfNotExistsAsync("Premium", plansConfig.GetSection("Premium"));
            await CreatePlanIfNotExistsAsync("Enterprise", plansConfig.GetSection("Enterprise"));
            await CreatePlanIfNotExistsAsync("Admin", plansConfig.GetSection("Admin"));

            _logger.LogInformation("Completed roles and plans setup");
        }

        private async Task CreatePlanIfNotExistsAsync(string planType, IConfigurationSection config)
        {
            if (config == null || string.IsNullOrEmpty(config["Name"]))
            {
                _logger.LogWarning("Configuration section for plan type {PlanType} is missing or invalid", planType);
                return;
            }

            var planName = config["Name"] ?? throw new ArgumentException("Plan name cannot be null or empty");
            var plan = await _subscriptionPlanRepository.GetByNameAsync(planName);
            if (plan == null)
            {
                _logger.LogInformation("Creating {PlanType} subscription plan with name {PlanName}", planType, planName);

                // Parse all configuration values
                if (!int.TryParse(config["LinkedUserLimit"], out int linkedUserLimit) ||
                    !bool.TryParse(config["Active"], out bool active) ||
                    !bool.TryParse(config["2FA"], out bool required2FA) ||
                    !bool.TryParse(config["EmailVerification"], out bool emailVerification) ||
                    !bool.TryParse(config["PremiumSupport"], out bool premiumSupport) ||
                    !bool.TryParse(config["Analytics"], out bool advancedAnalytics) ||
                    !double.TryParse(config["Price"], out double price))
                {
                    // Log detailed error if any configuration value is invalid
                    _logger.LogError("Invalid configuration values for {PlanType} plan. Values: LinkedUserLimit={LinkedUserLimit}, Active={Active}, 2FA={2FA}, EmailVerification={EmailVerification}, PremiumSupport={PremiumSupport}, Analytics={Analytics}, Price={Price}",
                        planType,
                        config["LinkedUserLimit"],
                        config["Active"],
                        config["2FA"],
                        config["EmailVerification"],
                        config["PremiumSupport"],
                        config["Analytics"],
                        config["Price"]);
                    return;
                }

                _logger.LogInformation("Creating plan with values: LinkedUserLimit={LinkedUserLimit}, Active={Active}, 2FA={2FA}, EmailVerification={EmailVerification}, PremiumSupport={PremiumSupport}, Analytics={Analytics}, Price={Price}",
                    linkedUserLimit,
                    active,
                    required2FA,
                    emailVerification,
                    premiumSupport,
                    advancedAnalytics,
                    price);

                // Create the plan with parsed configuration values
                await _subscriptionPlanRepository.CreatePlanAsync(
                    name: planName,
                    linkedUserLimit: linkedUserLimit,
                    active: active,
                    required2FA: required2FA,
                    emailVerification: emailVerification,
                    premiumSupport: premiumSupport,
                    advancedAnalytics: advancedAnalytics,
                    price: price
                );

                _logger.LogInformation("Successfully created plan {PlanName}", planName);
            }
            else
            {
                _logger.LogInformation("Plan {PlanName} already exists", planName);
            }
        }

        /// <summary>
        /// Gets the current role of a user.
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <returns>The user's current role or null if not found</returns>
        public async Task<string?> GetUserRoleAsync(string userId)
        {
            // Validate input
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetUserRoleAsync called with null or empty userId");
                return null;
            }

            // Find user in database
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found with id {UserId}", userId);
                return null;
            }

            // Get and return first role (users should only have one role)
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }

        /// <summary>
        /// Gets the count of linked users for a given user.
        /// </summary>
        /// <param name="userId">ID of the user to check</param>
        /// <returns>Number of linked users</returns>
        private async Task<int> GetLinkedUserCountAsync(string userId)
        {
            // Validate input
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetLinkedUserCountAsync called with null or empty userId");
                return 0;
            }

            // Get and count linked users
            var linkedUsers = await _linkedUserRepository.GetByCreatedByUserIdAsync(userId);
            return linkedUsers?.Count() ?? 0;
        }

        /// <summary>
        /// Gets the maximum number of linked users allowed for a user based on their subscription plan.
        /// </summary>
        /// <param name="userId">ID of the user to check</param>
        /// <returns>Maximum number of linked users allowed</returns>
        private async Task<int> GetUserLinkedUserLimitAsync(string userId)
        {
            // Validate input
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetUserLinkedUserLimitAsync called with null or empty userId");
                return 0;
            }

            // Find user in database
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found with id {UserId}", userId);
                return 0;
            }

            // Get linked user limit from claims
            var claims = await _userManager.GetClaimsAsync(user);
            var limitClaim = claims.FirstOrDefault(c => c.Type == "LinkedUserLimit");
            if (limitClaim == null || !int.TryParse(limitClaim.Value, out int limit))
            {
                _logger.LogWarning("LinkedUserLimit claim not found or invalid for user {UserId}", userId);
                return 0;
            }

            return limit;
        }

        /// <summary>
        /// Sets up subscription plan claims for a user based on their role.
        /// </summary>
        /// <param name="roleName">Role being assigned</param>
        /// <param name="planName">Name of the subscription plan</param>
        /// <param name="config">Configuration section for the plan</param>
        /// <param name="claims">List of claims to add to</param>
        /// <returns>True if setup was successful, false otherwise</returns>
        private async Task<bool> SetupSubscriptionPlanAsync(string roleName, string planName, IConfigurationSection config, List<Claim> claims)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(planName) || config == null)
            {
                _logger.LogWarning("Invalid planName or config for role {RoleName}", roleName);
                return false;
            }

            // Verify plan exists in database
            var subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync(planName);
            if (subscriptionPlan == null)
            {
                _logger.LogWarning("Subscription plan {PlanName} not found", planName);
                return false;
            }

            // Add subscription-related claims
            var linkedUserLimit = config["LinkedUserLimit"] ?? "0";
            claims.Add(new Claim("SubscriptionPlan", planName));
            claims.Add(new Claim("LinkedUserLimit", linkedUserLimit));
            claims.Add(new Claim("IsAdmin", (roleName == "AdminUser").ToString()));

            return true;
        }

        /// <summary>
        /// Assigns a linked user role and permissions to a user.
        /// </summary>
        /// <param name="userId">ID of the user to make a linked user</param>
        /// <param name="createdByUserId">ID of the user creating this linked user</param>
        /// <param name="permissions">Permissions to assign to the linked user</param>
        /// <returns>True if assignment was successful, false otherwise</returns>
        public async Task<bool> AssignLinkedUserAsync(string userId, string createdByUserId, LinkedUserPermissions permissions)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(createdByUserId))
            {
                return false;
            }

            // Find user in database
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Verify LinkedUser role exists
            var roleExists = await _roleManager.RoleExistsAsync("LinkedUser");
            if (!roleExists)
            {
                return false;
            }

            // Check if creator has reached their linked user limit
            var currentLinkedUsers = await GetLinkedUserCountAsync(createdByUserId);
            var linkedUserLimit = await GetUserLinkedUserLimitAsync(createdByUserId);

            if (currentLinkedUsers >= linkedUserLimit)
            {
                return false;
            }

            // Verify creator's group exists
            var creatorGroup = await _userGroupRepository.GetByUserIdAsync(createdByUserId);
            if (creatorGroup == null)
            {
                return false;
            }

            // Remove existing roles and claims
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            var existingClaims = await _userManager.GetClaimsAsync(user);
            await _userManager.RemoveClaimsAsync(user, existingClaims);

            // Assign LinkedUser role
            var result = await _userManager.AddToRoleAsync(user, "LinkedUser");
            if (!result.Succeeded)
            {
                return false;
            }

            // Create or update linked user record
            var existingLinkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
            if (existingLinkedUser == null)
            {
                // Create new linked user with permissions
                await _linkedUserRepository.CreateLinkedUserAsync(
                    userId,
                    createdByUserId,
                    permissions.CanPerformTransactions,
                    permissions.CanGenerateReports,
                    permissions.CanManageProducts,
                    permissions.CanAlterStock,
                    permissions.CanManagePromotions
                );
            }
            else
            {
                // Update existing linked user permissions
                await _linkedUserRepository.UpdateLinkedUserAsync(
                    existingLinkedUser.LinkedUserId,
                    permissions.CanPerformTransactions,
                    permissions.CanGenerateReports,
                    permissions.CanManageProducts,
                    permissions.CanAlterStock,
                    permissions.CanManagePromotions
                );
            }

            // Add linked user claims with permissions
            var claims = new List<Claim>
            {
                new Claim("IsLinkedUser", "true"),
                new Claim("CreatedByUserId", createdByUserId),
                new Claim("CanPerformTransactions", permissions.CanPerformTransactions.ToString()),
                new Claim("CanGenerateReports", permissions.CanGenerateReports.ToString()),
                new Claim("CanManageProducts", permissions.CanManageProducts.ToString()),
                new Claim("CanAlterStock", permissions.CanAlterStock.ToString()),
                new Claim("CanManagePromotions", permissions.CanManagePromotions.ToString())
            };

            // Add all claims to user
            var claimsResult = await _userManager.AddClaimsAsync(user, claims);
            return claimsResult.Succeeded;
        }

        /// <summary>
        /// Assigns a role to a user and manages their subscription plan.
        /// This method handles both the role assignment and subscription plan management in a transactional way.
        /// </summary>
        /// <param name="userId">The ID of the user to assign the role to</param>
        /// <param name="roleName">The name of the role to assign. Must be one of: FreeUser, StandardPlanUser, PremiumPlanUser, EnterprisePlanUser, AdminUser</param>
        /// <returns>True if the role was successfully assigned, false otherwise</returns>
        /// <remarks>
        /// The method performs the following steps:
        /// 1. Validates the user exists and the role is valid
        /// 2. Gets the corresponding subscription plan for the role
        /// 3. Validates user meets plan requirements (email verification, 2FA)
        /// 4. Creates or updates the user's group with the new plan
        /// 5. Assigns the role and updates claims
        /// 
        /// Role to Plan mapping:
        /// - FreeUser -> Free Plan
        /// - StandardPlanUser -> Standard Plan
        /// - PremiumPlanUser -> Premium Plan
        /// - EnterprisePlanUser -> Enterprise Plan
        /// - AdminUser -> Admin Plan
        /// 
        /// Requirements by plan:
        /// - Free: No requirements
        /// - Standard: Email verification
        /// - Premium: Email verification + 2FA
        /// - Enterprise: Email verification + 2FA
        /// - Admin: Email verification + 2FA
        /// </remarks>
        public async Task<bool> AssignRoleAsync(string userId, string roleName)
        {
            // Basic input validation to ensure we have required parameters
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleName))
            {
                return false;
            }

            // Find the user in the database
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Verify role exists and is not the special LinkedUser role
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists || roleName == "LinkedUser")
            {
                return false;
            }

            // Get the subscription plan based on role name
            SubscriptionPlan? subscriptionPlan = null;
            _logger.LogInformation("Getting subscription plan for role {RoleName}", roleName);
            switch (roleName)
            {
                // Map each role to its corresponding plan
                case "FreeUser":
                    _logger.LogInformation("Role is FreeUser, getting Free plan");
                    subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync("Free");
                    _logger.LogInformation("Free plan found: {PlanFound}", subscriptionPlan != null);
                    break;
                case "StandardPlanUser":
                    subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync("Standard");
                    break;
                case "PremiumPlanUser":
                    subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync("Premium");
                    break;
                case "EnterprisePlanUser":
                    subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync("Enterprise");
                    break;
                case "AdminUser":
                    subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync("Admin");
                    break;
            }

            // Ensure we found a valid subscription plan
            if (subscriptionPlan == null)
            {
                _logger.LogError("Subscription plan not found for role {RoleName}", roleName);
                return false;
            }

            _logger.LogInformation("Found subscription plan {PlanName} for role {RoleName}", subscriptionPlan.Name, roleName);

            // Check if user meets email verification requirement
            if (subscriptionPlan.RequiresEmailVerification && !user.EmailConfirmed)
            {
                _logger.LogWarning("User {UserId} attempted to get role {RoleName} but email is not verified", userId, roleName);
                return false;
            }

            // Check if user meets 2FA requirement
            if (subscriptionPlan.Requires2FA)
            {
                var twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
                if (!twoFactorEnabled)
                {
                    _logger.LogWarning("User {UserId} attempted to get role {RoleName} but 2FA is not enabled", userId, roleName);
                    return false;
                }
            }

            // Get current roles to handle transition
            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault();

            // Add new role first to ensure user is never without a role
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to add role {RoleName} to user {UserId}. Errors: {Errors}",
                    roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            // Remove old roles only after new one is successfully added
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError("Failed to remove old roles from user {UserId}. Errors: {Errors}",
                        userId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));

                    // Rollback - remove new role and restore old one
                    await _userManager.RemoveFromRoleAsync(user, roleName);
                    if (currentRole != null)
                    {
                        await _userManager.AddToRoleAsync(user, currentRole);
                    }
                    return false;
                }
            }

            // Clean up old claims before adding new ones
            var existingClaims = await _userManager.GetClaimsAsync(user);
            await _userManager.RemoveClaimsAsync(user, existingClaims);

            // Handle user group management for subscription
            var existingGroup = await _userGroupRepository.GetByUserIdAsync(userId);
            if (existingGroup == null)
            {
                // First time getting a plan - create new group
                await _userGroupRepository.CreateGroupAsync(userId, subscriptionPlan.Id);
                _logger.LogInformation("Created new group for user {UserId} with plan {PlanName}", userId, subscriptionPlan.Name);
            }
            else
            {
                // Update existing group with new plan
                await _userGroupRepository.UpdateGroupAsync(existingGroup.GroupId, subscriptionPlan.Id);
                _logger.LogInformation("Updated group {GroupId} for user {UserId} to plan {PlanName}", existingGroup.GroupId, userId, subscriptionPlan.Name);
            }

            // Setup claims for the user based on their role and subscription plan
            var claims = new List<Claim>();
            bool setupSuccess = true;

            switch (roleName)
            {
                case "FreeUser":
                    // Free users have basic claims with no subscription benefits
                    claims.Add(new Claim("SubscriptionPlan", "None"));
                    claims.Add(new Claim("LinkedUserLimit", "0"));
                    claims.Add(new Claim("IsAdmin", "false"));
                    break;

                case "StandardPlanUser":
                    // Setup standard plan claims with configured limits
                    var standardConfig = _configuration.GetSection("Plans:Standard");
                    setupSuccess = await SetupSubscriptionPlanAsync(roleName, "Standard", standardConfig, claims);
                    if (!setupSuccess) return false;
                    break;

                case "PremiumPlanUser":
                    // Setup premium plan claims with higher limits
                    var premiumConfig = _configuration.GetSection("Plans:Premium");
                    setupSuccess = await SetupSubscriptionPlanAsync(roleName, "Premium", premiumConfig, claims);
                    if (!setupSuccess) return false;
                    break;

                case "EnterprisePlanUser":
                    // Setup enterprise plan claims with maximum benefits
                    var enterpriseConfig = _configuration.GetSection("Plans:Enterprise");
                    setupSuccess = await SetupSubscriptionPlanAsync(roleName, "Enterprise", enterpriseConfig, claims);
                    if (!setupSuccess) return false;
                    break;

                case "AdminUser":
                    // Setup admin claims with full system access
                    var adminConfig = _configuration.GetSection("Plans:Admin");
                    setupSuccess = await SetupSubscriptionPlanAsync(roleName, "Admin", adminConfig, claims);
                    if (!setupSuccess) return false;
                    break;
            }

            // Finally, add all the claims to the user's identity
            var claimsResult = await _userManager.AddClaimsAsync(user, claims);
            return claimsResult.Succeeded;
        }

        public async Task<bool> CreateLinkedUserAsync(string createdByUserId, LinkedUserPermissions permissions)
        {
            if (string.IsNullOrEmpty(createdByUserId))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(createdByUserId);
            if (user == null)
            {
                return false;
            }

            var roleExists = await _roleManager.RoleExistsAsync("LinkedUser");
            if (!roleExists)
            {
                return false;
            }

            var result = await _userManager.AddToRoleAsync(user, "LinkedUser");
            if (!result.Succeeded)
            {
                return false;
            }

            await _linkedUserRepository.CreateLinkedUserAsync(
                createdByUserId, //Same ID as the user who created it, change if necessary
                createdByUserId,
                permissions.CanAlterStock,
                permissions.CanGenerateReports,
                permissions.CanManageProducts,
                permissions.CanManagePromotions,
                permissions.CanPerformTransactions
            );

            return true;

        }

        public async Task<bool> UpdateLinkedUserAsync(string userId, LinkedUserPermissions permissions)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
            if (linkedUser == null)
            {
                return false;
            }

            linkedUser.CanAlterStock = permissions.CanAlterStock;
            linkedUser.CanGenerateReports = permissions.CanGenerateReports;
            linkedUser.CanManageProducts = permissions.CanManageProducts;
            linkedUser.CanManagePromotions = permissions.CanManagePromotions;
            linkedUser.CanPerformTransactions = permissions.CanPerformTransactions;

            var updateResult = await _linkedUserRepository.UpdateLinkedUserAsync(
                linkedUser.LinkedUserId,
                linkedUser.CanAlterStock,
                linkedUser.CanGenerateReports,
                linkedUser.CanManageProducts,
                linkedUser.CanManagePromotions,
                linkedUser.CanPerformTransactions
            );

            if (updateResult == null)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> DeleteLinkedUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
            if (linkedUser == null)
            {
                return false;
            }

            var deleteResult = await _linkedUserRepository.DeleteLinkedUserAsync(linkedUser.LinkedUserId);

            return deleteResult;
        }
    }
}