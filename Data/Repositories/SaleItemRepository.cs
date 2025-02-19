using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class SaleItemRepository : ISaleItemRepository
    {
        private readonly CoreAPIContext _context;

        public SaleItemRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
