using Business.DataRepositories;
using Data.Context;

namespace Data.Repositories
{
    public class PaymentMethodRepository : IPaymentMethodRepository
    {
        private readonly CoreAPIContext _context;

        public PaymentMethodRepository(CoreAPIContext context)
        {
            _context = context;
        }
    }
}
