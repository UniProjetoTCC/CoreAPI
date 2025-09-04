using Business.Models;

namespace Business.DataRepositories
{
    public interface ISupplierPriceRepository
    {
        Task<SupplierPriceBusinessModel> CreateAsync(SupplierPriceBusinessModel price);
        Task<SupplierPriceBusinessModel?> GetByIdAsync(string id, string groupId);
        Task<List<SupplierPriceBusinessModel>> GetBySupplierIdAsync(string supplierId, string groupId);
        Task<SupplierPriceBusinessModel?> UpdateAsync(SupplierPriceBusinessModel price);
        Task<bool> DeleteAsync(string id, string groupId);
        Task<bool> DoesPriceExistForProductAsync(string supplierId, string productId, string groupId);
    }
}