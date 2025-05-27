using Microsoft.Extensions.Logging;
using Business.DataRepositories;
using Hangfire;

namespace Business.Jobs.Scheduled
{
    public class DeactivateExpiredGroupsJob
    {
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ILogger<DeactivateExpiredGroupsJob> _logger;

        public DeactivateExpiredGroupsJob(
            IUserGroupRepository userGroupRepository,
            ILogger<DeactivateExpiredGroupsJob> logger)
        {
            _userGroupRepository = userGroupRepository;
            _logger = logger;
        }

        [Queue("critical")]
        public async Task Execute()
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
    }
}
