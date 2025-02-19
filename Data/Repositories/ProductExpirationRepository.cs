using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class ProductExpirationRepository : IProductExpirationRepository
    {
        private readonly CoreAPIContext _context;

        public ProductExpirationRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
