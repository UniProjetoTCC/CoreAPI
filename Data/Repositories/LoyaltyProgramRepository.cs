using Business.DataRepositories;
using Data.Context; 

namespace Data.Repositories
{
    public class LoyaltyProgramRepository : ILoyaltyProgramRepository
    {
        private readonly CoreAPIContext _context;

        public LoyaltyProgramRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
