using Business.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Base
{
    public interface ISaleService
    {
        Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request, string userId, string groupId);
        Task<SaleBusinessModel?> CompleteSaleAsync(SaleRequest request, string userId, string groupId);
        Task<SaleBusinessModel?> GetByIdAsync(string id, string groupId);
        Task<SaleBusinessModel?> DeleteSaleAsync(string id, string groupId);
    }
}