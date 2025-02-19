using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class LinkedUserRepository : ILinkedUserRepository
    {
        private readonly CoreAPIContext _context;

        public LinkedUserRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
