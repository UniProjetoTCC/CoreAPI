using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly CoreAPIContext _context;

        public PromotionRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
