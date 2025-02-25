using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Business.DataRepositories;
using Business.Services.Base;

namespace Business.Jobs.Background
{
    public class UserDowngradeJob
    {
        private readonly string PROJECT_NAME;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserDowngradeJob> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;

        public UserDowngradeJob(
            IUserGroupRepository userGroupRepository,
            IEmailSenderService emailSenderService,
            IConfiguration configuration,
            ILogger<UserDowngradeJob> logger,
            UserManager<IdentityUser> userManager,
            ILinkedUserRepository linkedUserRepository,
            ISubscriptionPlanRepository subscriptionPlanRepository)
        {
            _userGroupRepository = userGroupRepository;
            _emailSenderService = emailSenderService;
            _configuration = configuration;
            _logger = logger;
            _userManager = userManager;
            _linkedUserRepository = linkedUserRepository;
            _subscriptionPlanRepository = subscriptionPlanRepository;

            PROJECT_NAME = _configuration["ProjectName"] ??
                throw new InvalidOperationException("ProjectName configuration is not set in appsettings.json!");
        }

        public async Task HandleUserDowngrade(int groupId)
        {
            try
            {
                var group = await _userGroupRepository.GetByGroupIdAsync(groupId);
                if (group == null)
                {
                    _logger.LogWarning("Group not found for ID: {GroupId}", groupId);
                    return;
                }

                var linkedUsers = await _linkedUserRepository.GetAllByGroupIdAsync(groupId);
                if (linkedUsers == null || !linkedUsers.Any())
                {
                    _logger.LogWarning("No linked users found for group ID: {GroupId}", groupId);
                    return;
                }

                var subscriptionPlan = await _subscriptionPlanRepository.GetByIdAsync(group.SubscriptionPlanId);
                if (subscriptionPlan == null)
                {
                    _logger.LogWarning("Subscription plan not found for group ID: {GroupId}", groupId);
                    return;
                }

                int limit = subscriptionPlan.LinkedUserLimit;
                if (limit == 0) return;

                if (linkedUsers.Count() > limit)
                {
                    var recentLinkedUsers = linkedUsers
                        .OrderByDescending(user => user.CreatedAt)
                        .Take(linkedUsers.Count() - limit)
                        .ToList();

                    foreach (var linkedUser in recentLinkedUsers)
                    {
                        var user = await _userManager.FindByIdAsync(linkedUser.LinkedUserId);
                        if (user?.Email == null)
                        {
                            _logger.LogWarning("User or email not found for linked user ID: {LinkedUserId}", linkedUser.Id);
                            continue;
                        }

                        var message = $@"
                            <h2>Subscription Downgrade Notice</h2>
                            <p>Hello {user.UserName ?? "User"},</p>
                            <p>Your account will be deactivated in 7 days due to exceed the linked user limit of your group subscription.</p>
                            <p>Unless the group subscription is upgraded, your account will be deactivated.</p>
                            <p>If you believe this is a mistake, you can contact our support team.</p>
                            <br>
                            <p>Best regards,</p>
                            <p>{PROJECT_NAME} Team</p>";

                        try
                        {
                            await _emailSenderService.SendEmailAsync(
                                email: user.Email,
                                subject: "Subscription Downgrade Notice",
                                message: message
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send email to user: {UserId}", user.Id);
                        }
                    }

                    await Task.Delay(TimeSpan.FromDays(7));

                    try
                    {
                        await _linkedUserRepository.DeactivateLinkedUsersAsync(recentLinkedUsers.Select(lu => lu.Id));
                        _logger.LogInformation("Successfully deactivated {Count} linked users for group {GroupId}", 
                            recentLinkedUsers.Count, groupId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to deactivate linked users for group {GroupId}", groupId);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle user downgrade for group {GroupId}", groupId);
                throw;
            }
        }
    }
}
