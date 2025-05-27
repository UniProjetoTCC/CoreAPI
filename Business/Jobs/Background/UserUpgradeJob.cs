using Business.DataRepositories;
using Business.Models;
using Business.Services.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Business.Jobs.Background
{
    public class UserUpgradeJob
    {
        private readonly string PROJECT_NAME;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserUpgradeJob> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly IBackgroundJobRepository _backgroundJobRepository;

        public UserUpgradeJob(
            IUserGroupRepository userGroupRepository,
            IEmailSenderService emailSenderService,
            IConfiguration configuration,
            ILogger<UserUpgradeJob> logger,
            UserManager<IdentityUser> userManager,
            ILinkedUserRepository linkedUserRepository,
            ISubscriptionPlanRepository subscriptionPlanRepository,
            IBackgroundJobRepository backgroundJobRepository)
        {
            _userGroupRepository = userGroupRepository;
            _emailSenderService = emailSenderService;
            _configuration = configuration;
            _logger = logger;
            _userManager = userManager;
            _linkedUserRepository = linkedUserRepository;
            _subscriptionPlanRepository = subscriptionPlanRepository;
            _backgroundJobRepository = backgroundJobRepository;
            PROJECT_NAME = _configuration["ProjectName"] ?? "UniProjeto";
        }

        /// <summary>
        /// Sends notification about upcoming upgrade and schedules the actual upgrade
        /// </summary>
        public async Task HandleUserUpgrade(string groupId)
        {
            try
            {
                var group = await _userGroupRepository.GetByGroupIdAsync(groupId);
                if (group == null)
                {
                    _logger.LogError("EXTREME ERROR - Group not found for ID: {GroupId}", groupId);
                    return;
                }

                var linkedUsers = await _linkedUserRepository.GetAllByGroupIdAsync(groupId);
                if (linkedUsers == null || !linkedUsers.Any())
                {
                    _logger.LogInformation("No linked users found for group ID: {GroupId}. Canceling upgrade.", groupId);
                    await CancelUpgradeJobsForGroup(groupId);
                    return;
                }

                var subscriptionPlan = await _subscriptionPlanRepository.GetByIdAsync(group.SubscriptionPlanId);
                if (subscriptionPlan == null)
                {
                    _logger.LogError("EXTREME ERROR - Subscription plan not found for ID: {PlanId}", group.SubscriptionPlanId);
                    return;
                }

                // Get inactive linked users that can be activated based on the plan limit
                var inactiveUsers = linkedUsers.Where(u => !u.IsActive).ToList();
                var activeUsers = linkedUsers.Where(u => u.IsActive).ToList();
                var availableSlots = subscriptionPlan.LinkedUserLimit - activeUsers.Count();
                var usersToActivate = inactiveUsers.Take(availableSlots).ToList();

                if (!usersToActivate.Any())
                {
                    _logger.LogInformation("No users to activate for group ID: {GroupId}. Plan limit reached or no inactive users.", groupId);
                    await CancelUpgradeJobsForGroup(groupId);
                    return;
                }

                await SendUpgradeNotificationEmail(group, usersToActivate, subscriptionPlan);

                // Execute the upgrade immediately
                await ExecuteUpgrade(groupId, usersToActivate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EXTREME ERROR - Failed to handle upgrade for group {GroupId}", groupId);
                throw;
            }
        }

        /// <summary>
        /// Executes the actual upgrade process
        /// </summary>
        public async Task ExecuteUpgrade(string groupId, List<LinkedUser> usersToActivate)
        {
            try
            {
                var group = await _userGroupRepository.GetByGroupIdAsync(groupId);
                if (group == null)
                {
                    _logger.LogError($"Group not found for ID: {groupId}");
                    return;
                }

                await _linkedUserRepository.ActivateLinkedUsersAsync(usersToActivate.Select(lu => lu.Id));
                _logger.LogInformation($"Successfully activated {usersToActivate.Count} linked users for group {groupId}");

                // Update job status to Executed
                var activeJobs = await _backgroundJobRepository.GetActiveJobsByGroupIdAsync(groupId);
                if (activeJobs != null)
                {
                    foreach (var job in activeJobs.Where(j => j.JobType == JobTypes.LinkedUsersActivation))
                    {
                        job.Status = "Executed";
                        job.ExecutedAt = DateTime.UtcNow;
                        await _backgroundJobRepository.UpdateAsync(job);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"EXTREME ERROR - Failed to execute upgrade for group {groupId}");
                throw;
            }
        }

        private async Task SendUpgradeNotificationEmail(UserGroup group, List<LinkedUser> usersToActivate, SubscriptionPlan plan)
        {
            try
            {
                var owner = await _userManager.FindByIdAsync(group.UserId);
                if (owner == null)
                {
                    _logger.LogError($"EXTREME ERROR - Owner not found for group {group.GroupId}");
                    return;
                }

                var usersIdentity = new List<IdentityUser>();
                foreach (var user in usersToActivate)
                {
                    var userIdentity = await _userManager.FindByIdAsync(user.LinkedUserId);
                    if (userIdentity != null)
                    {
                        usersIdentity.Add(userIdentity);
                    }
                }

                var emailSubject = $"{PROJECT_NAME} - Users Activation";
                var emailBody = $"Hello {owner.UserName},\n\n" +
                              $"According to your plan {plan.Name}, {usersToActivate.Count} user(s) will be activated.\n\n" +
                              $"Users to be activated:\n" +
                              string.Join("\n", usersIdentity.Select(u => $"- {u.Email}")) + "\n\n" +
                              $"Sincerely,\n{PROJECT_NAME} Team";

                if (owner.Email != null)
                {
                    await _emailSenderService.SendEmailAsync(owner.Email, emailSubject, emailBody);
                    _logger.LogInformation("Upgrade notification email sent to {Email}", owner.Email);
                }
                else
                {
                    _logger.LogWarning("Owner {OwnerId} has no email address", owner.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"EXTREME ERROR - Failed to send upgrade notification email for group {group.GroupId}");
                throw;
            }
        }

        private async Task CancelUpgradeJobsForGroup(string groupId)
        {
            try
            {
                var activeJobs = await _backgroundJobRepository.GetActiveJobsByGroupIdAsync(groupId);
                if (activeJobs != null)
                {
                    foreach (var job in activeJobs.Where(j => j.JobType == JobTypes.LinkedUsersActivation))
                    {
                        job.Status = "Cancelled";
                        await _backgroundJobRepository.UpdateAsync(job);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"EXTREME ERROR - Failed to cancel upgrade jobs for group {groupId}");
                throw;
            }
        }
    }
}