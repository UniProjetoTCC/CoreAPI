using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly CoreAPIContext _context;

        public StockRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
