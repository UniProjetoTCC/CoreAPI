using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Data.Repositories
{
    public class SaleRepository : ISaleRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public SaleRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        
        public async Task<SaleBusinessModel?> CreateSaleAsync(string userId, string groupId, string paymentMethodId, string? customerId, decimal total, List<SaleItemBusinessModel> items)
        {
            var paymentMethod = await _context.PaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == paymentMethodId && p.GroupId == groupId && p.Active);

            if (paymentMethod == null)
            {
                throw new ArgumentException("Payment method not found or inactive.");
            }

            if (!string.IsNullOrEmpty(customerId))
            {
                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == customerId && c.GroupId == groupId && c.IsActive);

                if (customer == null)
                {
                    throw new ArgumentException("Customer not found or inactive.");
                }
            }

            var sale = new SaleModel
            {
                UserId = userId,
                GroupId = groupId,
                PaymentMethodId = paymentMethodId,
                CustomerId = customerId,
                Total = total,
                SaleDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Sales.Add(sale);
            
            foreach (var item in items)
            {
                var saleItem = new SaleItemModel
                {
                    SaleId = sale.Id,
                    ProductId = item.ProductId,
                    GroupId = groupId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TotalAmount = item.TotalAmount,
                    Observation = item.Observation,
                    PromotionId = item.PromotionId,
                    PromotionName = item.PromotionName,
                    PromotionDiscountPercentage = item.PromotionDiscountPercentage,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SaleItems.Add(saleItem);
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(sale.Id, groupId);
        }
        
        public async Task<SaleBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.PaymentMethod)
                .Include(s => s.User)
                .Include(s => s.SaleItems!)
                    .ThenInclude(si => si.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.GroupId == groupId);

            if (sale == null)
            {
                return null;
            }
            
            return _mapper.Map<SaleBusinessModel>(sale);
        }
        
        public async Task<SaleBusinessModel?> DeleteSaleAsync(string id, string groupId)
        {
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .FirstOrDefaultAsync(s => s.Id == id && s.GroupId == groupId);

            if (sale == null)
            {
                return null;
            }

            var saleToReturn = await GetByIdAsync(id, groupId);

            if (sale.SaleItems != null)
            {
                _context.SaleItems.RemoveRange(sale.SaleItems);
            }

            _context.Sales.Remove(sale);
            await _context.SaveChangesAsync();
            return saleToReturn;
        }

        public async Task<(List<SaleBusinessModel> items, int totalCount, decimal totalAmount)> SearchSalesAsync(
            string groupId,
            DateTime startDate,
            DateTime endDate,
            string? customerId,
            string? userId,
            string? paymentMethodId,
            int page,
            int pageSize
        )
        {
            var endDateInclusive = endDate.Date.AddDays(1).AddTicks(-1);

            var query = _context.Sales
                .AsNoTracking()
                .Where(s => s.GroupId == groupId &&
                        s.SaleDate >= startDate &&
                        s.SaleDate <= endDateInclusive);

            if (!string.IsNullOrEmpty(customerId)) query = query.Where(s => s.CustomerId == customerId);
            if (!string.IsNullOrEmpty(userId)) query = query.Where(s => s.UserId == userId);
            if (!string.IsNullOrEmpty(paymentMethodId)) query = query.Where(s => s.PaymentMethodId == paymentMethodId);

            var totalCount = await query.CountAsync();
            var totalAmount = totalCount > 0 ? await query.SumAsync(s => s.Total) : 0;

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(s => s.Customer)
                .Include(s => s.PaymentMethod)
                .Include(s => s.User)
                .Include(s => s.SaleItems!)
                    .ThenInclude(si => si.Product)
                .ToListAsync();

            return (_mapper.Map<List<SaleBusinessModel>>(sales), totalCount, totalAmount);
        }
    }
}