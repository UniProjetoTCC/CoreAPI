using Quartz;
using System.Threading.Tasks;

namespace Business.Services.Base
{
    public interface IScheduledJobsService
    {
        /// <summary>
        /// Handles the deactivation of expired user groups.
        /// </summary>
        Task HandleDeactivateExpiredUserGroups(IJobExecutionContext context);

        /// <summary>
        /// Handles notifications for user groups that are about to expire.
        /// </summary>
        Task HandleNotifyExpiringUserGroups(IJobExecutionContext context);
    }
}
