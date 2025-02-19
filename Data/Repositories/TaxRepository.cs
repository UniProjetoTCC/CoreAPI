using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class TaxRepository : ITaxRepository
    {
        private readonly CoreAPIContext _context;

        public TaxRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
