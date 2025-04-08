using Business.Models;

namespace Business.DataRepositories
{
    public interface IBackgroundJobRepository
    {
        Task<HangJob> CreateAsync(HangJob job);
        Task<HangJob?> GetByHangfireJobIdAsync(string hangfireJobId);
        Task<IEnumerable<HangJob>?> GetActiveJobsByGroupIdAsync(string groupId);
        Task<HangJob?> UpdateAsync(HangJob job);
    }
}
