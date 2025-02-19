using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly CoreAPIContext _context;

        public ProductRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
