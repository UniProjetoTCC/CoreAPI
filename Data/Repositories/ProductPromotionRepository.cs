using Business.DataRepositories;
using Data.Models;
using Data.Context;

namespace Data.Repositories
{
    public class ProductPromotionRepository : IProductPromotionRepository
    {
        private readonly CoreAPIContext _context;

        public ProductPromotionRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
