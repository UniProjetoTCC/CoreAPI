using Microsoft.Extensions.Logging;
using Quartz;
using Business.Services.Base;

namespace Business.Jobs.Scheduled
{
    public class NotifyExpiringGroupsJob : IJob
    {
        private readonly IScheduledJobsService _scheduledJobsService;
        private readonly ILogger<NotifyExpiringGroupsJob> _logger;

        public NotifyExpiringGroupsJob(
            IScheduledJobsService scheduledJobsService,
            ILogger<NotifyExpiringGroupsJob> logger)
        {
            _scheduledJobsService = scheduledJobsService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _scheduledJobsService.HandleNotifyExpiringUserGroups(context);
        }
    }
}
