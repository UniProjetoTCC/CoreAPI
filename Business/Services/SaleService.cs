using Business.DataRepositories;
using Business.Models;
using Business.Services.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services
{
    public class SaleService : ISaleService
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductPromotionRepository _productPromotionRepository;
        private readonly IStockRepository _stockRepository;
        private readonly ILoyaltyProgramRepository _loyaltyProgramRepository;
        private readonly ILoyaltyPointsService _loyaltyPointsService;
        private readonly ILogger<SaleService> _logger;

        public SaleService(
            ISaleRepository saleRepository,
            ICustomerRepository customerRepository,
            IPaymentMethodRepository paymentMethodRepository,
            IProductRepository productRepository,
            IProductPromotionRepository productPromotionRepository,
            IStockRepository stockRepository,
            ILoyaltyProgramRepository loyaltyProgramRepository,
            ILoyaltyPointsService loyaltyPointsService,
            ILogger<SaleService> logger)
        {
            _saleRepository = saleRepository;
            _customerRepository = customerRepository;
            _paymentMethodRepository = paymentMethodRepository;
            _productRepository = productRepository;
            _productPromotionRepository = productPromotionRepository;
            _stockRepository = stockRepository;
            _loyaltyProgramRepository = loyaltyProgramRepository;
            _loyaltyPointsService = loyaltyPointsService;
            _logger = logger;
        }

        public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request, string userId, string groupId)
        {
            try
            {
                var response = new CheckoutResponse();
                var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.PaymentMethodId, groupId);
                if (paymentMethod == null) throw new ArgumentException($"Método de pagamento com ID {request.PaymentMethodId} não encontrado.");
                response.PaymentMethodName = paymentMethod.Name;

                CustomerBusinessModel? customer = null;
                LoyaltyProgramBusinessModel? loyaltyProgram = null;

                if (!string.IsNullOrEmpty(request.CustomerId))
                {
                    customer = await _customerRepository.GetByIdAsync(request.CustomerId, groupId);
                    if (customer == null) throw new ArgumentException($"Cliente com ID {request.CustomerId} não encontrado.");
                    if (customer.LoyaltyProgramId != null)
                    {
                        loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
                    }
                    response.Customer = new CustomerSummaryDto
                    {
                        Id = customer.Id, Name = customer.Name, Document = customer.Document,
                        LoyaltyPoints = customer.LoyaltyPoints, LoyaltyProgramName = loyaltyProgram?.Name,
                        LoyaltyDiscountPercentage = loyaltyProgram?.DiscountPercentage
                    };
                }

                var (hasSufficientStock, stockIssues) = await CheckStockAvailabilityAsync(request.Items, groupId);
                response.HasSufficientStock = hasSufficientStock;
                response.StockIssues = stockIssues;

                foreach (var item in request.Items)
                {
                    var product = await _productRepository.GetById(item.ProductId, groupId);
                    if (product == null) throw new ArgumentException($"Produto com ID {item.ProductId} não encontrado.");
                    var stock = await _stockRepository.GetByProductIdAsync(item.ProductId, groupId);

                    var checkoutItem = new CheckoutItemResponse
                    {
                        ProductId = product.Id, ProductName = product.Name, ProductBarCode = product.BarCode,
                        ProductSKU = product.SKU, Quantity = item.Quantity, UnitPrice = product.Price,
                        DiscountedUnitPrice = product.Price, TotalAmount = product.Price * item.Quantity,
                        Observation = item.Observation, StockAvailable = stock?.Quantity ?? 0
                    };

                    var allPromotions = await _productPromotionRepository.GetPromotionsByProductIdAsync(product.Id, groupId);
                    var activePromotions = allPromotions
                        .Where(p => p.IsActive && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow)
                        .ToList();
                    
                    checkoutItem.AvailablePromotions = activePromotions.Select(p => new PromotionAppliedDto
                    {
                        Id = p.Id, Name = p.Name, DiscountPercentage = p.DiscountPercentage
                    }).ToList();

                    PromotionBusinessModel? promotionToApply = null;

                    if (!string.IsNullOrEmpty(item.AppliedPromotionId))
                    {
                        promotionToApply = activePromotions.FirstOrDefault(p => p.Id == item.AppliedPromotionId);
                    }
                    else
                    {
                        promotionToApply = activePromotions.OrderByDescending(p => p.DiscountPercentage).FirstOrDefault();
                    }
                    
                    if (promotionToApply != null)
                    {
                        var discountAmount = Math.Round(product.Price * (promotionToApply.DiscountPercentage / 100), 2);
                        checkoutItem.DiscountedUnitPrice -= discountAmount;
                        checkoutItem.DiscountAmount += discountAmount * item.Quantity;
                        checkoutItem.DefaultPromotionApplied = new PromotionAppliedDto
                        {
                           Id = promotionToApply.Id, Name = promotionToApply.Name, DiscountPercentage = promotionToApply.DiscountPercentage
                        };
                    }
                    
                    if (request.ApplyLoyaltyDiscount && loyaltyProgram?.DiscountPercentage > 0)
                    {
                        var loyaltyDiscountAmount = Math.Round(checkoutItem.DiscountedUnitPrice * (loyaltyProgram.DiscountPercentage / 100), 2);
                        checkoutItem.DiscountedUnitPrice -= loyaltyDiscountAmount;
                        checkoutItem.DiscountAmount += loyaltyDiscountAmount * item.Quantity;
                    }
                    
                    checkoutItem.TotalAmount = checkoutItem.DiscountedUnitPrice * item.Quantity;
                    response.Items.Add(checkoutItem);
                    response.Subtotal += product.Price * item.Quantity;
                    response.TotalDiscounts += checkoutItem.DiscountAmount;
                }

                response.Total = response.Subtotal - response.TotalDiscounts;
                if (request.ApplyLoyaltyPoints && customer != null && loyaltyProgram?.CentsToPoints > 0)
                {
                    response.LoyaltyPointsToAdd = (int)Math.Floor((response.Total * 100) / loyaltyProgram.CentsToPoints);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar checkout da venda");
                throw;
            }
        }

        public async Task<SaleBusinessModel?> CompleteSaleAsync(SaleRequest request, string userId, string groupId)
        {
            try
            {
                var (hasSufficientStock, _) = await CheckStockAvailabilityAsync(request.Items.Select(i => new CheckoutItemRequest { ProductId = i.ProductId, Quantity = i.Quantity }).ToList(), groupId);
                if (!hasSufficientStock)
                {
                    throw new InvalidOperationException("Não há estoque suficiente para completar a venda.");
                }

                decimal finalTotal = 0;
                var saleItems = new List<SaleItemBusinessModel>();
                LoyaltyProgramBusinessModel? loyaltyProgram = null;

                if (!string.IsNullOrEmpty(request.CustomerId))
                {
                     var customer = await _customerRepository.GetByIdAsync(request.CustomerId, groupId);
                     if (customer?.LoyaltyProgramId != null)
                     {
                        loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
                     }
                }

                foreach (var item in request.Items)
                {
                    var product = await _productRepository.GetById(item.ProductId, groupId);
                    if (product == null) throw new ArgumentException($"Produto com ID {item.ProductId} não encontrado.");

                    decimal itemDiscountAmount = 0;
                    PromotionBusinessModel? appliedPromotion = null;

                    if (!string.IsNullOrEmpty(item.AppliedPromotionId))
                    {
                        var allPromotions = await _productPromotionRepository.GetPromotionsByProductIdAsync(product.Id, groupId);
                        appliedPromotion = allPromotions
                            .FirstOrDefault(p => p.Id == item.AppliedPromotionId && p.IsActive && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow);
                        
                        if (appliedPromotion != null)
                        {
                            itemDiscountAmount = Math.Round(product.Price * (appliedPromotion.DiscountPercentage / 100), 2);
                        }
                    }

                    decimal discountedPrice = product.Price - itemDiscountAmount;

                    if (request.ApplyLoyaltyDiscount && loyaltyProgram?.DiscountPercentage > 0)
                    {
                        var loyaltyDiscount = Math.Round(discountedPrice * (loyaltyProgram.DiscountPercentage / 100), 2);
                        itemDiscountAmount += loyaltyDiscount;
                    }
                    
                    var finalItemPrice = product.Price - itemDiscountAmount;
                    var totalItemAmount = finalItemPrice * item.Quantity;
                    finalTotal += totalItemAmount;

                    saleItems.Add(new SaleItemBusinessModel
                    {
                        ProductId = item.ProductId, GroupId = groupId, Quantity = item.Quantity,
                        UnitPrice = product.Price, DiscountAmount = itemDiscountAmount * item.Quantity,
                        TotalAmount = totalItemAmount, Observation = item.Observation ?? string.Empty,
                        PromotionId = appliedPromotion?.Id, PromotionName = appliedPromotion?.Name,
                        PromotionDiscountPercentage = appliedPromotion?.DiscountPercentage
                    });
                }
                
                var sale = await _saleRepository.CreateSaleAsync(userId, groupId, request.PaymentMethodId, request.CustomerId, finalTotal, saleItems);
                if (sale == null) throw new InvalidOperationException("Falha ao criar a venda.");

                foreach (var item in saleItems)
                {
                    await _stockRepository.DeductStockAsync(item.ProductId, groupId, item.Quantity, userId, $"Venda #{sale.Id}");
                }

                if (request.ApplyLoyaltyPoints && !string.IsNullOrEmpty(request.CustomerId) && loyaltyProgram?.CentsToPoints > 0)
                {
                    await _loyaltyPointsService.AddPointsAsync(request.CustomerId, finalTotal, groupId, $"Pontos da Venda #{sale.Id}");
                }

                return sale;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao completar venda");
                throw;
            }
        }

        public async Task<SaleBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            try
            {
                return await _saleRepository.GetByIdAsync(id, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting sale by ID {id}");
                throw;
            }
        }

        public async Task<SaleBusinessModel?> DeleteSaleAsync(string id, string groupId)
        {
            try
            {
                var saleToDelete = await _saleRepository.GetByIdAsync(id, groupId);
                if (saleToDelete == null)
                {
                    return null;
                }
                
                foreach (var item in saleToDelete.SaleItems)
                {
                    await _stockRepository.AddStockAsync(
                        item.ProductId,
                        groupId,
                        item.Quantity,
                        saleToDelete.UserId,
                        $"Reversal for deleted sale #{saleToDelete.Id}");
                }

                if (!string.IsNullOrEmpty(saleToDelete.CustomerId))
                {
                     var customer = await _customerRepository.GetByIdAsync(saleToDelete.CustomerId, groupId);
                     if (customer?.LoyaltyProgramId != null)
                     {
                        var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
                        if (loyaltyProgram?.CentsToPoints > 0)
                        {
                            await _loyaltyPointsService.RemovePointsAsync(
                                saleToDelete.CustomerId,
                                saleToDelete.Total,
                                groupId,
                                $"Point reversal for deleted sale #{saleToDelete.Id}");
                        }
                     }
                }

                var deletedSale = await _saleRepository.DeleteSaleAsync(id, groupId);
                
                return deletedSale;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting sale with ID {id}");
                throw;
            }
        }

        private async Task<(bool hasSufficientStock, List<StockIssueDto>? issues)> CheckStockAvailabilityAsync(List<CheckoutItemRequest> items, string groupId)
        {
            var issues = new List<StockIssueDto>();
            foreach (var item in items)
            {
                var stock = await _stockRepository.GetByProductIdAsync(item.ProductId, groupId);
                if (stock == null || stock.Quantity < item.Quantity)
                {
                    var product = await _productRepository.GetById(item.ProductId, groupId);
                    issues.Add(new StockIssueDto
                    {
                        ProductId = item.ProductId,
                        ProductName = product?.Name ?? "Produto desconhecido",
                        RequestedQuantity = item.Quantity,
                        AvailableQuantity = stock?.Quantity ?? 0
                    });
                }
            }
            return (issues.Count == 0, issues.Any() ? issues : null);
        }
    }
}