using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class SupplierPriceRepository : ISupplierPriceRepository
    {
        private readonly CoreAPIContext _context;

        public SupplierPriceRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
