using Hangfire;
using Business.Services.Base;
using Business.Jobs.Background;
using Business.DataRepositories;
using Business.Models;
using Microsoft.Extensions.Logging;

namespace Business.Services
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IBackgroundJobRepository _jobRepository;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(
            IBackgroundJobRepository jobRepository,
            ILogger<BackgroundJobService> logger)
        {
            _jobRepository = jobRepository;
            _logger = logger;
        }

        public async Task<string> EnqueueUserDowngrade(int groupId)
        {
            // First job sends warning email and schedules the actual downgrade
            var hangfireJobId = BackgroundJob.Enqueue<UserDowngradeJob>(
                job => job.HandleUserDowngrade(groupId));

            // Save job information
            var job = new HangJob
            {
                HangfireJobId = hangfireJobId,
                JobType = JobTypes.LinkedUsersDeactivation,
                GroupId = groupId,
                Status = "Scheduled"
            };

            await _jobRepository.CreateAsync(job);
            return hangfireJobId;
        }

        public async Task<string> EnqueueUserUpgrade(int groupId)
        {
            // First job sends notification email and executes the upgrade
            var hangfireJobId = BackgroundJob.Enqueue<UserUpgradeJob>(
                job => job.HandleUserUpgrade(groupId));

            // Save job information
            var job = new HangJob
            {
                HangfireJobId = hangfireJobId,
                JobType = JobTypes.LinkedUsersActivation,
                GroupId = groupId,
                Status = "Scheduled"
            };

            await _jobRepository.CreateAsync(job);
            return hangfireJobId;
        }

        public async Task<bool> CancelDowngrade(string jobId)
        {
            var job = await _jobRepository.GetByHangfireJobIdAsync(jobId);
            if (job == null || job.Status != "Scheduled")
            {
                return false;
            }

            var cancelled = BackgroundJob.Delete(jobId);
            if (cancelled)
            {
                job.Status = "Cancelled";
                job.CancelledAt = DateTime.UtcNow;
                var result = await _jobRepository.UpdateAsync(job);
                if (result == null)
                {
                    return false;
                }
                _logger.LogInformation("Job {JobId} for group {GroupId} was cancelled", jobId, job.GroupId);
            }

            return cancelled;
        }

        public async Task<IEnumerable<HangJob>?> GetActiveJobsByGroupId(int groupId)
        {
            return await _jobRepository.GetActiveJobsByGroupIdAsync(groupId);
        }

        public async Task<bool> UpdateJobStatus(string jobId, string status)
        {
            var job = await _jobRepository.GetByHangfireJobIdAsync(jobId);
            if (job == null)
            {
                _logger.LogError("Job {JobId} not found", jobId);
                return false;
            }

            job.Status = status;
            if (status == "Executed")
            {
                job.ExecutedAt = DateTime.UtcNow;
            }
            else if (status == "Cancelled")
            {
                job.CancelledAt = DateTime.UtcNow;
            }

            var result = await _jobRepository.UpdateAsync(job);
            if (result == null)
            {
                _logger.LogError($"Failed to update job {jobId} status to {status}");
                return false;
            }

            _logger.LogInformation($"Job {jobId} status updated to {status}");
            return true;
        }
    }
}
