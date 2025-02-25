using Microsoft.Extensions.Logging;
using Business.DataRepositories;
using Business.Services.Base;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Business.Models;

namespace Business.Jobs.Scheduled
{
    public class NotifyExpiringGroupsJob
    {
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IEmailSenderService _emailSenderService;
        private readonly ILogger<NotifyExpiringGroupsJob> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private const string PROJECT_NAME = "UniProjetoTCC";

        public NotifyExpiringGroupsJob(
            IUserGroupRepository userGroupRepository,
            IEmailSenderService emailSenderService,
            ILogger<NotifyExpiringGroupsJob> logger,
            UserManager<IdentityUser> userManager)
        {
            _userGroupRepository = userGroupRepository;
            _emailSenderService = emailSenderService;
            _logger = logger;
            _userManager = userManager;
        }

        [Queue("normal")]
        public async Task Execute()
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
