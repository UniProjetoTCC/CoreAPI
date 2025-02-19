using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly CoreAPIContext _context;

        public CustomerRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
