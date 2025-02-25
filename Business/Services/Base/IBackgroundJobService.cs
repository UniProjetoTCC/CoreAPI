using Business.Models;

namespace Business.Services.Base
{
    public interface IBackgroundJobService
    {
        /// <summary>
        /// Enqueues a job to handle user downgrade process. 
        /// This will send a warning email and schedule the actual downgrade for 7 days later.
        /// </summary>
        /// <param name="groupId">The ID of the group to downgrade</param>
        /// <returns>The job ID of the scheduled downgrade (can be used to cancel)</returns>
        Task<string> EnqueueUserDowngrade(int groupId);

        /// <summary>
        /// Cancels a scheduled downgrade if it hasn't been executed yet
        /// </summary>
        /// <param name="jobId">The ID of the scheduled downgrade job</param>
        /// <returns>True if the downgrade was cancelled, false if it wasn't found or already executed</returns>
        Task<bool> CancelDowngrade(string jobId);

        /// <summary>
        /// Gets all active (scheduled) jobs for a specific group
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <returns>List of active background jobs</returns>
        Task<IEnumerable<HangJob>?> GetActiveJobsByGroupId(int groupId);
    }
}
