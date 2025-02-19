using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly CoreAPIContext _context;

        public PurchaseOrderRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
