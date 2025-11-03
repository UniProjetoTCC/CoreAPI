using Business.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.DataRepositories
{
    public interface ISupplierPriceRepository
    {
        Task<SupplierPriceBusinessModel?> GetByIdAsync(string id, string groupId);

        Task<(IEnumerable<SupplierPriceBusinessModel> Items, int TotalCount)> GetPricesForProductAsync(
            string productId, 
            string groupId, 
            int page, 
            int pageSize);

        Task<(IEnumerable<SupplierPriceBusinessModel> Items, int TotalCount)> GetPricesFromSupplierAsync(
            string supplierId, 
            string groupId, 
            int page, 
            int pageSize);

        Task<bool> CheckDuplicateAsync(string productId, string supplierId, string supplierSku, string groupId, string? excludeId = null);

        Task<SupplierPriceBusinessModel> CreateAsync(SupplierPriceBusinessModel price);

        Task<SupplierPriceBusinessModel?> UpdateAsync(string id, string groupId, SupplierPriceBusinessModel price);

        Task<SupplierPriceBusinessModel?> ActivateAsync(string id, string groupId);

        Task<SupplierPriceBusinessModel?> DeactivateAsync(string id, string groupId);

        Task<bool> DeleteAsync(string id, string groupId);
    }
}