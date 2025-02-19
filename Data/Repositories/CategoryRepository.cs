using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly CoreAPIContext _context;

        public CategoryRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
