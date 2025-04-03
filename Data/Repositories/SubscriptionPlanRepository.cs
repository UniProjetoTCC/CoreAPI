using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public SubscriptionPlanRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<SubscriptionPlan?> GetByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Name == name);

            return plan != null ? _mapper.Map<SubscriptionPlan>(plan) : null;
        }

        public async Task<SubscriptionPlan?> GetByIdAsync(string id)
        {
            var plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == id);

            return plan != null ? _mapper.Map<SubscriptionPlan>(plan) : null;
        }

        public async Task CreatePlanAsync(
            string name,
            int linkedUserLimit,
            bool active,
            bool required2FA,
            bool emailVerification,
            bool premiumSupport,
            bool advancedAnalytics,
            double price)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            }

            var plan = new SubscriptionPlanModel
            {
                Name = name,
                LinkedUserLimit = linkedUserLimit,
                IsActive = active,
                Requires2FA = required2FA,
                RequiresEmailVerification = emailVerification,
                HasPremiumSupport = premiumSupport,
                HasAdvancedAnalytics = advancedAnalytics,
                MonthlyPrice = (decimal)price,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();
        }
    }
}
