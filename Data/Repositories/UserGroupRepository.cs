using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class UserGroupRepository : IUserGroupRepository
    {
        private readonly CoreAPIContext _context;

        public UserGroupRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
