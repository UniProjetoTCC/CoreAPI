using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class PurchaseOrderItemRepository : IPurchaseOrderItemRepository
    {
        private readonly CoreAPIContext _context;

        public PurchaseOrderItemRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
