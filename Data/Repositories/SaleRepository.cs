using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class SaleRepository : ISaleRepository
    {
        private readonly CoreAPIContext _context;

        public SaleRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
