using Microsoft.Extensions.Logging;
using Quartz;
using Business.Services.Base;

namespace Business.Jobs.Scheduled
{
    public class DeactivateExpiredGroupsJob : IJob
    {
        private readonly IScheduledJobsService _scheduledJobsService;
        private readonly ILogger<DeactivateExpiredGroupsJob> _logger;

        public DeactivateExpiredGroupsJob(
            IScheduledJobsService scheduledJobsService,
            ILogger<DeactivateExpiredGroupsJob> logger)
        {
            _scheduledJobsService = scheduledJobsService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _scheduledJobsService.HandleDeactivateExpiredUserGroups(context);
        }
    }
}
