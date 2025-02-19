using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class PriceHistoryRepository : IPriceHistoryRepository
    {
        private readonly CoreAPIContext _context;

        public PriceHistoryRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
