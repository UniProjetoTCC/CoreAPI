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
    public class RoleService : IRoleService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RoleService> _logger;

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

        public async Task SetupRolesAndPlansAsync()
        {
            _logger.LogInformation("Starting roles and plans setup");

            // Setup Roles
            var roles = new[]
            {
                "StandardUser",
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
                _logger.LogInformation("Creating {PlanType} subscription plan", planType);

                if (!int.TryParse(config["LinkedUserLimit"], out int linkedUserLimit) ||
                    !bool.TryParse(config["Active"], out bool active) ||
                    !bool.TryParse(config["2FA"], out bool required2FA) ||
                    !bool.TryParse(config["EmailVerification"], out bool emailVerification) ||
                    !bool.TryParse(config["PremiumSupport"], out bool premiumSupport) ||
                    !bool.TryParse(config["Analytics"], out bool advancedAnalytics) ||
                    !double.TryParse(config["Price"], out double price))
                {
                    _logger.LogError("Invalid configuration values for {PlanType} plan", planType);
                    return;
                }

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
            }
        }

        public async Task<string?> GetUserRoleAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetUserRoleAsync called with null or empty userId");
                return null;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found with id {UserId}", userId);
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }

        private async Task<int> GetLinkedUserCountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetLinkedUserCountAsync called with null or empty userId");
                return 0;
            }

            var linkedUsers = await _linkedUserRepository.GetByCreatedByUserIdAsync(userId);
            return linkedUsers?.Count() ?? 0;
        }

        private async Task<int> GetUserLinkedUserLimitAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetUserLinkedUserLimitAsync called with null or empty userId");
                return 0;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found with id {UserId}", userId);
                return 0;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            var limitClaim = claims.FirstOrDefault(c => c.Type == "LinkedUserLimit");
            if (limitClaim == null || !int.TryParse(limitClaim.Value, out int limit))
            {
                _logger.LogWarning("LinkedUserLimit claim not found or invalid for user {UserId}", userId);
                return 0;
            }

            return limit;
        }

        private async Task<bool> SetupSubscriptionPlanAsync(string roleName, string planName, IConfigurationSection config, List<Claim> claims)
        {
            if (string.IsNullOrEmpty(planName) || config == null)
            {
                _logger.LogWarning("Invalid planName or config for role {RoleName}", roleName);
                return false;
            }

            var subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync(planName);
            if (subscriptionPlan == null)
            {
                _logger.LogWarning("Subscription plan {PlanName} not found", planName);
                return false;
            }

            var linkedUserLimit = config["LinkedUserLimit"] ?? "0";
            claims.Add(new Claim("SubscriptionPlan", planName));
            claims.Add(new Claim("LinkedUserLimit", linkedUserLimit));
            claims.Add(new Claim("IsAdmin", (roleName == "AdminUser").ToString()));

            return true;
        }

        public async Task<bool> AssignRoleAsync(
            string userId,
            string roleName,
            string? createdByUserId = null,
            bool transactions = false,
            bool reports = false,
            bool manage = false,
            bool stock = false,
            bool promotions = false
        )
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleName))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
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

            // Add new role and claims based on role type
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                return false;
            }

            var claims = new List<Claim>();
            SubscriptionPlan? subscriptionPlan = null;
            bool setupSuccess = true;

            switch (roleName)
            {
                case "StandardUser":
                    claims.Add(new Claim("SubscriptionPlan", "None"));
                    claims.Add(new Claim("LinkedUserLimit", "0"));
                    claims.Add(new Claim("IsAdmin", "false"));
                    break;

                case "StandardPlanUser":
                    var standardConfig = _configuration.GetSection("Plans:Standard");
                    setupSuccess = await SetupSubscriptionPlanAsync(roleName, "Standard", standardConfig, claims);
                    if (!setupSuccess) return false;
                    subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync("Standard");
                    break;

                case "PremiumPlanUser":
                    var premiumConfig = _configuration.GetSection("Plans:Premium");
                    setupSuccess = await SetupSubscriptionPlanAsync(roleName, "Premium", premiumConfig, claims);
                    if (!setupSuccess) return false;
                    subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync("Premium");
                    break;

                case "EnterprisePlanUser":
                    var enterpriseConfig = _configuration.GetSection("Plans:Enterprise");
                    setupSuccess = await SetupSubscriptionPlanAsync(roleName, "Enterprise", enterpriseConfig, claims);
                    if (!setupSuccess) return false;
                    subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync("Enterprise");
                    break;

                case "AdminUser":
                    var adminConfig = _configuration.GetSection("Plans:Admin");
                    setupSuccess = await SetupSubscriptionPlanAsync(roleName, "Admin", adminConfig, claims);
                    if (!setupSuccess) return false;
                    subscriptionPlan = await _subscriptionPlanRepository.GetByNameAsync("Admin");
                    break;

                case "LinkedUser":
                    if (string.IsNullOrEmpty(createdByUserId))
                    {
                        return false;
                    }

                    var currentLinkedUsers = await GetLinkedUserCountAsync(createdByUserId);
                    var linkedUserLimit = await GetUserLinkedUserLimitAsync(createdByUserId);

                    if (currentLinkedUsers >= linkedUserLimit)
                    {
                        return false;
                    }

                    var creatorGroup = await _userGroupRepository.GetByUserIdAsync(createdByUserId);
                    if (creatorGroup == null)
                    {
                        return false;
                    }

                    var existingGroup = await _userGroupRepository.GetByUserIdAsync(userId);
                    if (existingGroup == null)
                    {
                        await _userGroupRepository.CreateGroupAsync(userId, creatorGroup.SubscriptionPlanId);
                    }
                    else
                    {
                        await _userGroupRepository.UpdateGroupAsync(existingGroup.GroupId, creatorGroup.SubscriptionPlanId);
                    }

                    // Create or update LinkedUser record
                    var existingLinkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
                    if (existingLinkedUser == null)
                    {
                        await _linkedUserRepository.CreateLinkedUserAsync(
                            userId,
                            createdByUserId,
                            transactions,
                            reports,
                            manage,
                            stock,
                            promotions
                        );
                    }
                    else
                    {
                        await _linkedUserRepository.UpdateLinkedUserAsync(
                            existingLinkedUser.Id,
                            transactions,
                            reports,
                            manage,
                            stock,
                            promotions
                        );
                    }

                    claims.Add(new Claim("IsLinkedUser", "true"));
                    claims.Add(new Claim("CreatedByUserId", createdByUserId));
                    claims.Add(new Claim("CanPerformTransactions", transactions.ToString()));
                    claims.Add(new Claim("CanGenerateReports", reports.ToString()));
                    claims.Add(new Claim("CanManageProducts", manage.ToString()));
                    claims.Add(new Claim("CanAlterStock", stock.ToString()));
                    claims.Add(new Claim("CanManagePromotions", promotions.ToString()));
                    break;
            }

            // Handle group management for subscription plans
            if (subscriptionPlan != null && roleName != "LinkedUser")
            {
                var existingGroup = await _userGroupRepository.GetByUserIdAsync(userId);
                if (existingGroup == null)
                {
                    // First time getting a plan - create new group
                    await _userGroupRepository.CreateGroupAsync(userId, subscriptionPlan.Id);
                }
                else
                {
                    // Updating existing plan
                    await _userGroupRepository.UpdateGroupAsync(existingGroup.GroupId, subscriptionPlan.Id);
                }
            }

            var claimsResult = await _userManager.AddClaimsAsync(user, claims);
            return claimsResult.Succeeded;
        }
    }
}