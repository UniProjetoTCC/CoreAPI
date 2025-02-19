using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class ProductTaxRepository : IProductTaxRepository
    {
        private readonly CoreAPIContext _context;

        public ProductTaxRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
