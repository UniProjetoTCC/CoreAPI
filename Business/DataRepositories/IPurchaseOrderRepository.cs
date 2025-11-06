using Business.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.DataRepositories
{
    public interface IPurchaseOrderRepository
    {
        Task<PurchaseOrderBusinessModel?> GetByIdAsync(string id, string groupId);
        Task<PurchaseOrderBusinessModel> CreateAsync(PurchaseOrderBusinessModel order);
        Task<PurchaseOrderBusinessModel?> UpdateAsync(PurchaseOrderBusinessModel order);
        Task<bool> DeleteAsync(string id, string groupId);
        Task<PurchaseOrderBusinessModel?> GetByOrderNumberAsync(string orderNumber, string groupId);
        
        Task<(List<PurchaseOrderBusinessModel> Items, int TotalCount)> SearchAsync(
            string groupId,
            DateTime startDate,
            DateTime endDate,
            string? supplierId,
            string? status,
            int page,
            int pageSize
        );
    }
}