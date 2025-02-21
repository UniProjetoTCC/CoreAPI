using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business.Models;

namespace Business.DataRepositories
{
    public interface ISubscriptionPlanRepository
    {
        Task<SubscriptionPlan?> GetByNameAsync(string name);
        Task CreatePlanAsync(string name, int linkedUserLimit, bool active, bool required2FA, bool emailVerification, bool premiumSupport, bool advancedAnalytics, double price);
    }
}