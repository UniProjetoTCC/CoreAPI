using Business.Models;

namespace Business.DataRepositories
{
    public interface ISubscriptionPlanRepository
    {
        Task<SubscriptionPlan?> GetByNameAsync(string name);
        Task<SubscriptionPlan?> GetByIdAsync(string id);
        Task CreatePlanAsync(string name, int linkedUserLimit, bool active, bool required2FA, bool emailVerification, bool premiumSupport, bool advancedAnalytics, double price);
    }
}