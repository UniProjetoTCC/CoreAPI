using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly CoreAPIContext _context;

        public SupplierRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
