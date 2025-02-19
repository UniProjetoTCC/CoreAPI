using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class UserPaymentCardRepository : IUserPaymentCardRepository
    {
        private readonly CoreAPIContext _context;

        public UserPaymentCardRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
