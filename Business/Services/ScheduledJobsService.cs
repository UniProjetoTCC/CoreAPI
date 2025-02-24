using Quartz;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Business.Services.Base;
using Business.DataRepositories;

namespace Business.Services
{
    public class ScheduledJobsService : IScheduledJobsService
    {
        private readonly string PROJECT_NAME;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ScheduledJobsService> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public ScheduledJobsService(
            IUserGroupRepository userGroupRepository,
            IEmailSenderService emailSenderService,
            IConfiguration configuration,
            ILogger<ScheduledJobsService> logger,
            UserManager<IdentityUser> userManager)
        {
            _userGroupRepository = userGroupRepository;
            _emailSenderService = emailSenderService;
            _configuration = configuration;
            _logger = logger;
            _userManager = userManager;

            PROJECT_NAME = _configuration["ProjectName"] ??
                throw new InvalidOperationException("ProjectName configuration is not set in appsettings.json!");
        }

        public async Task HandleDeactivateExpiredUserGroups(IJobExecutionContext context)
        {
            try
            {
                await _userGroupRepository.DeactivateExpiredUserGroups();
                _logger.LogInformation("Successfully deactivated expired user groups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate expired user groups");
                throw;
            }
        }

        public async Task HandleNotifyExpiringUserGroups(IJobExecutionContext context)
        {
            try
            {
                var expiringGroupsByDays = await _userGroupRepository.GetExpiringUserGroups();

                foreach (var days in expiringGroupsByDays.Keys)
                {
                    var groups = expiringGroupsByDays[days];
                    foreach (var group in groups)
                    {
                        var groupOwner = await _userManager.FindByIdAsync(group.UserId);
                        if (groupOwner?.Email == null)
                        {
                            _logger.LogWarning("User or email not found for group {GroupId}", group.GroupId);
                            continue;
                        }

                        var message = $@"
                            <h2>Subscription Expiration Notice</h2>
                            <p>Hello {groupOwner.UserName ?? "User"},</p>
                            <p>Your subscription for group {group.GroupId} will expire in {days} day(s). Please renew your subscription to avoid interruptions.</p>
                            <br>
                            <p>Best regards,</p>
                            <p>{PROJECT_NAME} Team</p>";

                        try
                        {
                            await _emailSenderService.SendEmailAsync(
                                email: groupOwner.Email,
                                subject: $"Subscription Expiration Notice in {days} days",
                                message: message
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send email to user {UserId}", groupOwner.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify expiring user groups");
                throw;
            }
        }
    }
}
