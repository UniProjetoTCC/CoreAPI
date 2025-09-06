using Business.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.DataRepositories
{
    public interface ISaleRepository
    {
        Task<SaleBusinessModel?> CreateSaleAsync(string userId, string groupId, string paymentMethodId, string? customerId, decimal total, List<SaleItemBusinessModel> items);
        Task<SaleBusinessModel?> GetByIdAsync(string id, string groupId);
        Task<SaleBusinessModel?> DeleteSaleAsync(string id, string groupId);
        
        Task<(List<SaleBusinessModel> items, int totalCount, decimal totalAmount)> SearchSalesAsync(
            string groupId,
            DateTime startDate,
            DateTime endDate,
            string? customerId,
            string? userId,
            string? paymentMethodId,
            int page,
            int pageSize
        );
    }
}