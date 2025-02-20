using Business.DataRepositories;
using Data.Models;

namespace Data.Repositories
{
    public class ProductPromotionRepository : BaseRepository<ProductPromotionModel>, IProductPromotionRepository
    {
        public ProductPromotionRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
