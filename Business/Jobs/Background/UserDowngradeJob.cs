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
        private readonly IBackgroundJobRepository _backgroundJobRepository;

        public UserDowngradeJob(
            IUserGroupRepository userGroupRepository,
            IEmailSenderService emailSenderService,
            IConfiguration configuration,
            ILogger<UserDowngradeJob> logger,
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
            PROJECT_NAME = configuration["ProjectName"] ?? throw new InvalidOperationException("ProjectName not configured");
        }

        /// <summary>
        /// Sends notification about upcoming downgrade and schedules the actual downgrade
        /// </summary>
        public async Task HandleUserDowngrade(int groupId)
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
                    _logger.LogInformation("No linked users found for group ID: {GroupId}. Canceling downgrade.", groupId);
                    await CancelDowngradeJobsForGroup(groupId);
                    return;
                }

                var subscriptionPlan = await _subscriptionPlanRepository.GetByIdAsync(group.SubscriptionPlanId);
                if (subscriptionPlan == null)
                {
                    _logger.LogError("EXTREME ERROR - Subscription plan not found for group ID: {GroupId}", groupId);
                    return;
                }

                // Check if the group has more users than the plan limit
                var linkedUserCount = linkedUsers.Count();
                if (linkedUserCount <= subscriptionPlan.LinkedUserLimit)
                {
                    _logger.LogInformation(
                        "Group {GroupId} has {CurrentUsers} users, which is within the plan limit of {MaxUsers}. Canceling downgrade.",
                        groupId, linkedUserCount, subscriptionPlan.LinkedUserLimit);
                    await CancelDowngradeJobsForGroup(groupId);
                    return;
                }

                // if the group has more users than the plan limit, send the downgrade warning email
                var excessUsers = linkedUsers.OrderByDescending(u => u.CreatedAt)
                                          .Take(linkedUserCount - subscriptionPlan.LinkedUserLimit)
                                          .ToList();

                var owner = await _userManager.FindByIdAsync(group.UserId);
                if (owner != null)
                {
                    var excessUserCount = excessUsers.Count();
                    await SendDowngradeWarningEmail(owner.Email!, excessUserCount);
                }

                // Schedule the actual downgrade
                BackgroundJob.Schedule(
                    (UserDowngradeJob job) => job.ExecuteDowngrade(groupId, excessUsers),
                    TimeSpan.FromDays(7));

                _logger.LogInformation($"Scheduled downgrade for {excessUsers.Count()} users in group {groupId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"EXTREME ERROR - Failed to handle user downgrade for group {groupId}");
                throw;
            }
        }

        private async Task CancelDowngradeJobsForGroup(int groupId)
        {
            try
            {
                var jobs = await _backgroundJobRepository.GetActiveJobsByGroupIdAsync(groupId);
                if (jobs != null)
                {
                    foreach (var job in jobs)
                    {
                        BackgroundJob.Delete(job.HangfireJobId);
                        job.Status = "Cancelled";
                        job.CancelledAt = DateTime.UtcNow;
                        await _backgroundJobRepository.UpdateAsync(job);
                    }
                }
                _logger.LogInformation($"Cancelled all downgrade jobs for group {groupId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"EXTREME ERROR - Failed to cancel downgrade jobs for group {groupId}");
                throw;
            }
        }

        /// <summary>
        /// Sends notification about upcoming downgrade
        /// </summary>
        private async Task SendDowngradeWarningEmail(string email, int excessUsers)
        {
            var message = $@"
                <h2>Subscription Downgrade Notice</h2>
                <p>Hello,</p>
                <p>Your account will be deactivated in 7 days due to exceed the linked user limit of your group subscription.</p>
                <p>Unless the group subscription is upgraded, your account will be deactivated.</p>
                <p>If you believe this is a mistake, you can contact our support team.</p>
                <br>
                <p>Best regards,</p>
                <p>{PROJECT_NAME} Team</p>";

            try
            {
                await _emailSenderService.SendEmailAsync(
                    email: email,
                    subject: "Subscription Downgrade Notice",
                    message: message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to user: {email}");
            }
        }

        /// <summary>
        /// Executes the actual downgrade process
        /// </summary>
        public async Task ExecuteDowngrade(int groupId, List<LinkedUser> recentLinkedUsers)
        {
            try
            {
                var group = await _userGroupRepository.GetByGroupIdAsync(groupId);
                if (group == null)
                {
                    _logger.LogError($"Group not found for ID: {groupId}");
                    return;
                }

                await _linkedUserRepository.DeactivateLinkedUsersAsync(recentLinkedUsers.Select(lu => lu.Id));
                _logger.LogInformation($"Successfully deactivated {recentLinkedUsers.Count} linked users for group {groupId}");

                // Update job status to Executed
                var activeJobs = await _backgroundJobRepository.GetActiveJobsByGroupIdAsync(groupId);
                if (activeJobs != null)
                {
                    foreach (var job in activeJobs.Where(j => j.JobType == JobTypes.LinkedUsersDeactivation))
                    {
                        job.Status = "Executed";
                        job.ExecutedAt = DateTime.UtcNow;
                        await _backgroundJobRepository.UpdateAsync(job);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"EXTREME ERROR - Failed to execute downgrade for group {groupId}");
                throw;
            }
        }
    }
}

