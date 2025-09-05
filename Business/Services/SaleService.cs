using Business.DataRepositories;
using Business.Models;
using Business.Services.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Services.Base
{
    public class SaleService : ISaleService
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IProductRepository _productRepository;
        private readonly ISaleItemRepository _saleItemRepository;
        private readonly IStockRepository _stockRepository;
        private readonly ILogger<SaleService> _logger;

        public SaleService(
            ISaleRepository saleRepository,
            ICustomerRepository customerRepository,
            IPaymentMethodRepository paymentMethodRepository,
            IProductRepository productRepository,
            ISaleItemRepository saleItemRepository,
            IStockRepository stockRepository,
            ILogger<SaleService> logger)
        {
            _saleRepository = saleRepository;
            _customerRepository = customerRepository;
            _paymentMethodRepository = paymentMethodRepository;
            _productRepository = productRepository;
            _saleItemRepository = saleItemRepository;
            _stockRepository = stockRepository;
            _logger = logger;
        }

        
    }
}
