using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly CoreAPIContext _context;

        public SubscriptionPlanRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
