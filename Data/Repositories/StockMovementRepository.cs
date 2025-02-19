using   Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly CoreAPIContext _context;

        public StockMovementRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
